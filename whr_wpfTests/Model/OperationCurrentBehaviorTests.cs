using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class OperationCurrentBehaviorTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PurchaseUseReleaseAndSaleTrackAvailableUnits(bool defaultStock)
        {
            var game = Game();
            IComposition stock = defaultStock
                ? new DefautltComposition { Price = 100 }
                : new Composition { Vehicles = new Dictionary<Car, int> { [new Car { money = 50 }] = 2 } };
            stock.Purchase(game, 3);
            Assert.AreEqual((999700L, 3), (game.Money, stock.HeldUnits));
            stock.Use(2);
            Assert.AreEqual(1, stock.HeldUnits);
            stock.Release(1);
            Assert.AreEqual(2, stock.HeldUnits);
            stock.Sale(game, 1);
            // ADR 0002 / F05: 原作の売却代金を資金へ加算する。
            Assert.AreEqual((999710L, 1), (game.Money, stock.HeldUnits));
            Assert.ThrowsException<StockShortageException>(() => stock.Use(2));
            Assert.ThrowsException<ArgumentException>(() => stock.Use(-1));
            Assert.ThrowsException<StockShortageException>(() => stock.Sale(game, 2));
            Assert.AreEqual((999710L, 1), (game.Money, stock.HeldUnits));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailedPurchaseLeavesMoneyAndAvailableUnitsIntact(bool defaultStock)
        {
            var game = Game();
            game.Money = 199;
            IComposition stock = defaultStock
                ? new DefautltComposition { Price = 100 }
                : new Composition { Vehicles = new Dictionary<Car, int> { [new Car { money = 100 }] = 1 } };
            Assert.ThrowsException<MoneyShortException>(() => stock.Purchase(game, 2));
            Assert.AreEqual((199L, 0), (game.Money, stock.HeldUnits));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NegativePurchaseIsRejectedBeforeChangingState(bool defaultStock)
        {
            var game = Game();
            IComposition stock = defaultStock
                ? new DefautltComposition { Price = 100 }
                : new Composition { Vehicles = new Dictionary<Car, int> { [new Car { money = 100 }] = 1 } };
            // ADR 0004: 数量の負数は状態変更前に拒否する。
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stock.Purchase(game, -1));
            Assert.AreEqual((1000000L, 0), (game.Money, stock.HeldUnits));
        }

        [TestMethod]
        public void VehicleDevelopmentChargesAndRegistersVehicleOrLeavesStateOnFailure()
        {
            var game = Game();
            game.genkaiJoki = 60;
            game.DevelopVehicle("試験車両", 60, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None);
            var car = game.vehicles.Single();
            Assert.AreEqual(("試験車両", 60, PowerEnum.Steam, RailTypeEnum.Iron, 1226),
                (car.Name, car.bestSpeed, car.power, car.type, car.money));
            Assert.AreEqual(987740L, game.Money);
            Assert.ThrowsException<VehicleDevelopmentRejectedException>(() => game.DevelopVehicle("速すぎる", 65, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None));
            game.Money = 0;
            Assert.ThrowsException<MoneyShortException>(() => game.DevelopVehicle("買えない", 60, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None));
            Assert.AreEqual(1, game.vehicles.Count);
            Assert.AreEqual(0L, game.Money);
        }

        [TestMethod]
        public void ConstructionSetsInfrastructureAndAccountsForCost()
        {
            var game = Game();
            var line = Line(game, false);
            line.bestSpeedUpKaisu = 3;
            long cost = line.CalcConstructCost(80, 2, RailTypeEnum.Iron, RailGaugeEnum.Regular, true, TaihisenEnum.Every50km, game);
            // 現状の代表建設費を実行結果から記録する。費用計算自体の変更も検出する。
            Assert.AreEqual(49820L, cost);
            line.Construct(80, RailTypeEnum.Iron, true, RailGaugeEnum.Regular, 2, TaihisenEnum.Every50km, game);
            Assert.AreEqual(1000000L - cost, game.Money);
            Assert.AreEqual(-cost, line.totalBalance);
            Assert.AreEqual((true, 80, 0, RailTypeEnum.Iron, (bool?)true, RailGaugeEnum.Regular, 2, TaihisenEnum.Every50km),
                (line.IsExist, line.bestSpeed, line.bestSpeedUpKaisu, line.Type, line.IsElectrified, line.gauge, line.LaneNum, line.taihisen));
        }

        [TestMethod]
        public void InvalidOrUnaffordableConstructionLeavesInfrastructureUntouched()
        {
            var game = Game();
            var line = Line(game, false);
            game.Money = 0;
            var before = Capture(line);
            Assert.ThrowsException<InvalidOperationException>(() => line.Construct(20, RailTypeEnum.Iron, false, RailGaugeEnum.Narrow, 2, TaihisenEnum.None, game));
            Assert.AreEqual(before, Capture(line));
            Assert.ThrowsException<MoneyShortException>(() => line.Construct(60, RailTypeEnum.Iron, false, RailGaugeEnum.Narrow, 2, TaihisenEnum.None, game));
            Assert.AreEqual(before, Capture(line));
            Assert.AreEqual(0L, game.Money);
        }

        [DataTestMethod]
        [DataRow("electrify")]
        [DataRow("unelectrify")]
        [DataRow("narrow")]
        [DataRow("regular")]
        [DataRow("remove")]
        public void InfrastructureChangesReleaseDirectAndThroughAllocations(string operation)
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            var through = Through(game, line);
            through.SettingComposition(stock, 60, game);
            long money = game.Money;
            if (operation == "unelectrify") line.IsElectrified = true;
            if (operation == "narrow") line.gauge = RailGaugeEnum.Regular;
            if (operation == "remove") line.LaneNum = 1;
            long cost = operation switch
            {
                "electrify" => line.CalcElectrifyCost(game),
                "narrow" => line.CalcNarrowGaugeCost(game),
                "regular" => line.CalcExpanseGaugeCost(game), _ => 0
            };
            switch (operation)
            {
                case "electrify": line.Electrify(game); break;
                case "unelectrify": line.UnElectrify(game); break;
                case "narrow": line.NarrowGauge(game); break;
                case "regular": line.ExpanseGauge(game); break;
                case "remove": line.ReduceOrRemoveLane(game); break;
            }
            Assert.AreEqual((money - cost, -cost, 10), (game.Money, line.totalBalance, stock.HeldUnits));
            Assert.IsNull(line.useComposition);
            Assert.IsNull(through.useComposition);
            Assert.AreEqual((0, 0, 0, 0), (line.useCompositionNum, line.runningPerDay, through.useCompositionNum, through.runningPerDay));
            if (operation == "electrify") Assert.AreEqual(true, line.IsElectrified);
            if (operation == "unelectrify") Assert.AreEqual(false, line.IsElectrified);
            if (operation == "narrow") Assert.AreEqual(RailGaugeEnum.Narrow, line.gauge);
            if (operation == "regular") Assert.AreEqual(RailGaugeEnum.Regular, line.gauge);
            if (operation == "remove") Assert.AreEqual((false, 60, 1, (bool?)false), (line.IsExist, line.bestSpeed, line.LaneNum, line.IsElectrified));
        }

        [DataTestMethod]
        [DataRow("electrify")]
        [DataRow("regular")]
        [DataRow("speed")]
        [DataRow("lane")]
        [DataRow("taihi")]
        public void UnaffordableInfrastructureChangePreservesAllocations(string operation)
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            line.bestSpeed = 200;
            line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            var through = Through(game, line);
            through.SettingComposition(stock, 30, game);
            var before = Capture(line);
            int available = stock.HeldUnits;
            game.Money = 0;
            Assert.ThrowsException<MoneyShortException>(() =>
            {
                switch (operation)
                {
                    case "electrify": line.Electrify(game); break;
                    case "regular": line.ExpanseGauge(game); break;
                    case "speed": line.SpeedUp(game); break;
                    case "lane": line.AddLane(game); break;
                    case "taihi": line.ChangeTaihi(TaihisenEnum.Every2km, game); break;
                }
            });
            Assert.AreEqual(before, Capture(line));
            Assert.AreEqual(available, stock.HeldUnits);
            Assert.AreSame(stock, through.useComposition);
            Assert.AreEqual((1, 30), (through.useCompositionNum, through.runningPerDay));
            Assert.AreEqual(0L, game.Money);
        }

        [DataTestMethod]
        [DataRow(1, 2, DiagramType.LimittedExpressPrior)]
        [DataRow(2, 4, DiagramType.Parallel)]
        [DataRow(4, 6, DiagramType.Parallel)]
        public void AddingLanesChangesDiagramAndPreservesAllocatedStock(int before, int after, DiagramType diagram)
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = before;
            var stock = Stock(game);
            line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            long money = game.Money;
            long cost = line.CalcAddLaneCost(game);
            line.AddLane(game);
            Assert.AreEqual((after, diagram, TaihisenEnum.None), (line.LaneNum, line.diagram, line.taihisen));
            Assert.AreEqual(money - cost, game.Money);
            Assert.AreEqual(-cost, line.totalBalance);
            Assert.AreSame(stock, line.useComposition);
            Assert.AreEqual((8, 2, 60), (stock.HeldUnits, line.useCompositionNum, line.runningPerDay));
        }

        [DataTestMethod]
        [DataRow(2, 1)]
        [DataRow(4, 2)]
        [DataRow(6, 4)]
        public void ReducingLanesResetsOperationsWithoutRefund(int before, int after)
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = before;
            var stock = Stock(game);
            line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            long money = game.Money;
            line.ReduceOrRemoveLane(game);
            Assert.AreEqual((after, true, money, 10), (line.LaneNum, line.IsExist, game.Money, stock.HeldUnits));
            Assert.IsNull(line.useComposition);
        }

        [TestMethod]
        public void SpeedAndTaihiChangesChargeWithoutReleasingStock()
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            long money = game.Money;
            long cost = line.CalcSpeedUpCost(game);
            line.SpeedUp(game);
            Assert.AreEqual((70, 1, money - cost, -cost), (line.bestSpeed, line.bestSpeedUpKaisu, game.Money, line.totalBalance));
            money = game.Money;
            cost = line.CalcTaihisenChangeCost(TaihisenEnum.Every2km, game);
            line.ChangeTaihi(TaihisenEnum.Every2km, game);
            Assert.AreEqual(money - cost, game.Money);
            Assert.AreEqual(TaihisenEnum.Every2km, line.taihisen);
            Assert.AreEqual((8, 2, 60), (stock.HeldUnits, line.useCompositionNum, line.runningPerDay));
        }

        // 失敗時に変更され得る路線状態をまとめて比較する。
        private static object Capture(Line line) => (
            line.IsExist, line.Type, line.IsElectrified, line.gauge, line.bestSpeed, line.bestSpeedUpKaisu,
            line.LaneNum, line.taihisen, line.diagram, line.totalBalance, line.useComposition,
            line.useCompositionNum, line.runningPerDay);
    }
}
