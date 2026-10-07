using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("SelectedBehavior")]
    public class SelectedBehaviorTests
    {
        [TestMethod]
        public void SuspensionKeepsEquipmentAndHistoryAndReleasesAllAllocations()
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = 1;
            line.bestSpeedUpKaisu = 3;
            line.retentionRate = 18000;
            line.totalBalance = -12345;
            var stock = Stock(game);
            line.SettingComposition(stock, 10, DiagramType.LimittedExpressPrior, game);
            var through = Through(game, line);
            through.SettingComposition(stock, 5, game);
            var unused = Through(game, line);
            var equipment = (line.bestSpeed, line.bestSpeedUpKaisu, line.Type, line.gauge,
                line.IsElectrified, line.LaneNum, line.taihisen, line.diagram);
            long money = game.Money;
            line.ReduceOrRemoveLane(game);
            Assert.AreEqual((false, true), (line.IsExist, line.IsSuspended));
            Assert.AreEqual(equipment, (line.bestSpeed, line.bestSpeedUpKaisu, line.Type, line.gauge,
                line.IsElectrified, line.LaneNum, line.taihisen, line.diagram));
            Assert.AreEqual((money, -12345L, 18000, 10), (game.Money, line.totalBalance, line.retentionRate, stock.HeldUnits));
            Assert.AreEqual((0, 0), (line.runningPerDay, line.useCompositionNum));
            Assert.IsNull(line.useComposition);
            foreach (var diagram in new[] { through, unused })
            {
                Assert.IsNull(diagram.useComposition);
                Assert.AreEqual((0, 0), (diagram.runningPerDay, diagram.useCompositionNum));
            }
            Assert.AreEqual(0, line.GenkaiHonsuuUnderCurrent(game.genkaikyoyo));
            Assert.ThrowsException<LineEquipmentRejectedException>(() => line.ReduceOrRemoveLane(game));
            Assert.AreEqual(10, stock.HeldUnits);
            game.NextWeek();
            Assert.AreEqual((0, 0, 0L, 0L), (line.passengersLastWeek, line.kamotsuNumLastWeek, line.incomeLastWeek, line.outlayLastWeek));
            Assert.AreEqual(money, game.Money);
        }

        [TestMethod]
        public void SuspendedRouteRejectsThroughAllocationWithoutChangingState()
        {
            var game = Game();
            var first = Line(game);
            var second = Line(game);
            second.LaneNum = 1;
            var stock = Stock(game);
            var through = Through(game, first, second);
            second.ReduceOrRemoveLane(game);
            var diagrams = (first.diagram, second.diagram);
            Assert.ThrowsException<ServiceSettingRejectedException>(() => through.SettingComposition(stock, 10, game));
            Assert.ThrowsException<ServiceSettingRejectedException>(() => second.SettingComposition(stock, 10, DiagramType.Regular, game));
            Assert.AreEqual(diagrams, (first.diagram, second.diagram));
            Assert.AreEqual((10, 0, 0), (stock.HeldUnits, through.runningPerDay, through.useCompositionNum));
            Assert.IsNull(through.useComposition);
        }

        [TestMethod]
        public void ApplyingModeClearsPreviousSuspension()
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = 1;
            line.ReduceOrRemoveLane(game);
            var mode = game.SelectedMode;
            mode.LineSettings.Add(new Mode.LineDefaultSetting
            {
                IsExist = true, Type = RailTypeEnum.Iron, gauge = RailGaugeEnum.Narrow,
                IsElectrified = false, bestSpeed = 60, LaneNum = 2, retentionRate = 10000,
                taihisen = TaihisenEnum.Every20km
            });
            game.SelectedMode = mode;
            Assert.AreEqual((true, false, 2), (line.IsExist, line.IsSuspended, line.LaneNum));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NegativeQuantitiesAreRejectedAndZeroIsANoOp(bool defaultStock)
        {
            var game = Game();
            IComposition stock = defaultStock ? new DefautltComposition { Price = 100 }
                : new Composition { Vehicles = new Dictionary<Car, int> { [new Car { money = 100 }] = 1 } };
            stock.Purchase(game, 3);
            long money = game.Money;
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stock.Purchase(game, -1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stock.Release(-1));
            Assert.ThrowsException<ArgumentException>(() => stock.Use(-1));
            Assert.ThrowsException<ArgumentException>(() => stock.Sale(game, -1));
            Assert.AreEqual((money, 3), (game.Money, stock.HeldUnits));
            stock.Purchase(game, 0);
            stock.Release(0);
            stock.Use(0);
            stock.Sale(game, 0);
            Assert.AreEqual((money, 3), (game.Money, stock.HeldUnits));
            game.Money = -100;
            stock.Purchase(game, 0);
            Assert.AreEqual((-100L, 3), (game.Money, stock.HeldUnits));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NegativeFrequencyIsRejectedBeforeChangingExistingAllocation(bool throughService)
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            var through = Through(game, line);
            if (throughService) through.SettingComposition(stock, 10, game);
            else line.SettingComposition(stock, 10, DiagramType.Regular, game);
            var before = (stock.HeldUnits, line.diagram, line.runningPerDay, line.useCompositionNum,
                through.runningPerDay, through.useCompositionNum);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                if (throughService) through.SettingComposition(stock, -1, game);
                else line.SettingComposition(stock, -1, DiagramType.Parallel, game);
            });
            Assert.AreEqual(before, (stock.HeldUnits, line.diagram, line.runningPerDay, line.useCompositionNum,
                through.runningPerDay, through.useCompositionNum));
        }

        [DataTestMethod]
        [DataRow(400, 613190000L, 0L, 400)]
        [DataRow(400, 613190001L, 0L, 410)]
        [DataRow(360, 0L, 1000000000L, 360)]
        public void AdvancedDieselImprovementDependsOnlyOnDieselInvestment(int level, long dieselInvestment, long electricInvestment, int expected)
        {
            var game = Game();
            game.genkaiKidosha = level;
            game.genkaiDenki = 100;
            game.AccumulatedInvest.diesel = dieselInvestment;
            game.AccumulatedInvest.electricMotor = electricInvestment;
            game.NextWeek();
            Assert.AreEqual(expected, game.genkaiKidosha);
        }
    }
}
