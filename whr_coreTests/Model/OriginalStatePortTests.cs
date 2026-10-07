using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    // ADR 0002で採用した原作由来の状態変更。旧挙動は変更前のGit履歴に残す。
    [TestClass]
    [TestCategory("AdoptedOriginalState")]
    public class OriginalStatePortTests
    {
        [TestMethod]
        public void WeeklyStateClearsEvenForUnbuiltLinesAndKeepsHistoricalState()
        {
            var game = Game();
            var line = Line(game, false);
            line.passengersLastWeek = 999;
            line.incomeLastWeek = 888;
            line.outlayLastWeek = 777;
            line.kamotsuNumLastWeek = 666;
            line.isOverCapacity = true;
            line.totalBalance = 123456;
            line.retentionRate = 15000;
            game.NextWeek();
            Assert.AreEqual((0, 0L, 0L, 0, false),
                (line.passengersLastWeek, line.incomeLastWeek, line.outlayLastWeek, line.kamotsuNumLastWeek, line.isOverCapacity));
            Assert.AreEqual((123456L, 15000), (line.totalBalance, line.retentionRate));
            Assert.AreEqual((1000000L, 0, 0), (game.Money, game.income, game.outlay));
        }

        [TestMethod]
        public void NoFreightWeekClearsPreviousExcessWithoutChangingAllocation()
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = 10;
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 90, DiagramType.LimittedExpressPrior, game);
            game.longwayList.Add(new Longway { start = line.Start, end = line.End, route = new List<Line> { line } });
            game.NextWeek();
            Assert.AreEqual((10, true), (line.kamotsuNumLastWeek, line.isOverCapacity));
            game.Kamotu = KamotsuEnum.Nothing;
            game.NextWeek();
            Assert.AreEqual((0, false, 90, 2, 8),
                (line.kamotsuNumLastWeek, line.isOverCapacity, line.runningPerDay, line.useCompositionNum, stock.HeldUnits));
            Assert.AreSame(stock, line.useComposition);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DisabledFreightRouteDoesNotEraseSharedAllowedRoute(bool reverse)
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = 10;
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 10, DiagramType.LimittedExpressPrior, game);
            var allowed = new Longway { start = line.Start, end = line.End, route = new List<Line> { line } };
            var disabled = new Longway { start = line.Start, end = line.End, route = new List<Line> { line }, isKamotuOperated = false };
            game.longwayList.AddRange(reverse ? new[] { disabled, allowed } : new[] { allowed, disabled });
            Assert.AreEqual(0, disabled.CalcKamotuTrips(game));
            Assert.AreEqual(22, allowed.CalcKamotuTrips(game));
            game.NextWeek();
            Assert.AreEqual(22, line.kamotsuNumLastWeek);
            Assert.IsTrue(line.passengersLastWeek > 0);
            // 禁止指定が旅客経路を無効にしないことを、禁止経路だけでも確認する。
            game.longwayList.Remove(allowed);
            int directPassengers = line.CalcPassengersNumOnlyLine(true, game);
            game.NextWeek();
            Assert.AreEqual(0, line.kamotsuNumLastWeek);
            Assert.IsTrue(line.passengersLastWeek > directPassengers);
        }

        [DataTestMethod]
        [DataRow(10, 10, 80, 26, 74, false)]
        [DataRow(30, 10, 80, 100, 0, true)]
        public void WartimeDeficitPassesFromDirectToThroughService(int freightSize, int directTrips, int throughTrips,
            int freightAfter, int throughAfter, bool overCapacity)
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = freightSize;
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, directTrips, DiagramType.LimittedExpressPrior, game);
            var through = Through(game, line);
            through.SettingComposition(stock, throughTrips, game);
            game.longwayList.Add(new Longway { start = line.Start, end = line.End, route = new List<Line> { line } });
            game.modss = new GameInfo.WarMode { kamotsuIndex = 120, EndYear = 1885 };
            int held = stock.HeldUnits;
            int directUnits = line.useCompositionNum;
            int throughUnits = through.useCompositionNum;
            game.NextWeek();
            Assert.AreEqual((freightAfter, 0, throughAfter, overCapacity),
                (line.kamotsuNumLastWeek, line.runningPerDay, through.runningPerDay, line.isOverCapacity));
            Assert.AreEqual((held, directUnits, throughUnits), (stock.HeldUnits, line.useCompositionNum, through.useCompositionNum));
        }

        [TestMethod]
        public void WartimeDeficitContinuesAcrossMultipleThroughServices()
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = 10;
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 10, DiagramType.LimittedExpressPrior, game);
            var first = Through(game, line);
            first.SettingComposition(stock, 5, game);
            var second = Through(game, line);
            second.SettingComposition(stock, 75, game);
            game.longwayList.Add(new Longway { start = line.Start, end = line.End, route = new List<Line> { line } });
            game.modss = new GameInfo.WarMode { kamotsuIndex = 120, EndYear = 1885 };
            int held = stock.HeldUnits;
            game.NextWeek();
            Assert.AreEqual((26, 0, 0, 74, false),
                (line.kamotsuNumLastWeek, line.runningPerDay, first.runningPerDay, second.runningPerDay, line.isOverCapacity));
            Assert.AreEqual(held, stock.HeldUnits);
        }

        [DataTestMethod]
        [DataRow(35, 490)]
        [DataRow(45, 630)]
        public void FreightFareUsesBothOriginalUnitPriceEndpoints(int unitPrice, int expected)
        {
            var game = Game();
            var line = Line(game);
            line.Distance = 1500;
            line.kamotsuNumLastWeek = 2;
            Assert.AreEqual(expected, line.CalcKamotsuFare(unitPrice));
        }

        [DataTestMethod]
        [DataRow(false, 0, 0L)]
        [DataRow(false, 1, 1L)]
        [DataRow(false, 2, 3L)]
        [DataRow(false, 3, 5L)]
        [DataRow(true, 0, 0L)]
        [DataRow(true, 1, 1L)]
        [DataRow(true, 2, 3L)]
        [DataRow(true, 3, 5L)]
        public void SaleRoundsTheTotalAndDoesNotCountAsWeeklyIncome(bool defaultStock, int quantity, long proceeds)
        {
            var game = Game();
            IComposition stock = SaleStock(game, defaultStock);
            long money = game.Money;
            Assert.AreEqual(proceeds, stock.CalcSalePrice(quantity));
            stock.Sale(game, quantity);
            Assert.AreEqual((money + proceeds, 3 - quantity), (game.Money, stock.HeldUnits));
            Assert.AreEqual((0, 0), (game.income, game.outlay));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InvalidSaleLeavesMoneyAndUnitsUnchanged(bool defaultStock)
        {
            var game = Game();
            IComposition stock = SaleStock(game, defaultStock);
            long money = game.Money;
            Assert.ThrowsException<StockShortageException>(() => stock.Sale(game, 4));
            Assert.AreEqual((money, 3), (game.Money, stock.HeldUnits));
            Assert.ThrowsException<ArgumentException>(() => stock.Sale(game, -1));
            Assert.AreEqual((money, 3), (game.Money, stock.HeldUnits));
            Assert.ThrowsException<ArgumentNullException>(() => stock.Sale(null!, 1));
            Assert.AreEqual((money, 3), (game.Money, stock.HeldUnits));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void BulkSaleMultipliesWithoutIntOverflow(bool defaultStock)
        {
            var game = Game();
            IComposition stock = SaleStock(game, defaultStock);
            if (stock is DefautltComposition preset) preset.Price = 1000000000;
            else ((Composition)stock).Vehicles.Single().Key.money = 1000000000;
            long money = game.Money;
            Assert.AreEqual(300000000L, stock.CalcSalePrice(3));
            stock.Sale(game, 3);
            Assert.AreEqual((money + 300000000L, 0), (game.Money, stock.HeldUnits));
        }

        [TestMethod]
        public void ResetReleasesAllocatedUnitsOnlyOnce()
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game);
            through.SettingComposition(stock, 60, game);
            through.DiagramReset();
            through.DiagramReset();
            Assert.AreEqual(10, stock.HeldUnits);
            Assert.IsNull(through.useComposition);
            Assert.AreEqual((0, 0), (through.useCompositionNum, through.runningPerDay));
        }

        [TestMethod]
        public void InfrastructureResetHandlesMixedAndSharedThroughAllocations()
        {
            var game = Game();
            var first = Line(game);
            var second = Line(game);
            var unused = Through(game, first, second);
            var operated = Through(game, first, second);
            var stock = Stock(game);
            operated.SettingComposition(stock, 60, game);
            first.Electrify(game);
            second.Electrify(game);
            Assert.AreEqual(10, stock.HeldUnits);
            foreach (var through in new[] { unused, operated })
            {
                Assert.IsNull(through.useComposition);
                Assert.AreEqual((0, 0), (through.useCompositionNum, through.runningPerDay));
            }
            Assert.AreEqual((true, true), (first.IsElectrified, second.IsElectrified));
        }

    }
}
