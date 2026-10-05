using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class AllocationCurrentBehaviorTests
    {
        [TestMethod]
        public void DirectServiceReusesStockAndReturnsOldStockOnReplacement()
        {
            var game = Game();
            var line = Line(game);
            var first = Stock(game);
            var second = Stock(game);
            long money = game.Money;
            line.SettingComposition(first, 60, DiagramType.LimittedExpressPrior, game);
            Assert.AreEqual((8, 2, 60), (first.HeldUnits, line.useCompositionNum, line.runningPerDay));
            line.SettingComposition(first, 120, DiagramType.LimittedExpressPrior, game);
            Assert.AreEqual((7, 3, 120), (first.HeldUnits, line.useCompositionNum, line.runningPerDay));
            line.SettingComposition(second, 60, DiagramType.Regular, game);
            Assert.AreEqual((10, 8, 2, 60, DiagramType.Regular),
                (first.HeldUnits, second.HeldUnits, line.useCompositionNum, line.runningPerDay, line.diagram));
            Assert.AreSame(second, line.useComposition);
            Assert.AreEqual(money, game.Money);
            line.SettingComposition(second, 0, DiagramType.Regular, game);
            Assert.AreEqual((10, 0, 0), (second.HeldUnits, line.useCompositionNum, line.runningPerDay));
        }

        [TestMethod]
        public void IncompatibleOrCompletelyUnavailableStockLeavesDirectServiceIntact()
        {
            var game = Game();
            var line = Line(game);
            var oldStock = Stock(game);
            line.SettingComposition(oldStock, 60, DiagramType.LimittedExpressPrior, game);
            var newStock = Stock(game, 0);
            Assert.ThrowsException<InvalidOperationException>(() => line.SettingComposition(newStock, 60, DiagramType.Regular, game));
            newStock.Power = PowerEnum.Electricity;
            Assert.ThrowsException<CompositionNotAppliedException>(() => line.SettingComposition(newStock, 60, DiagramType.Regular, game));
            Assert.AreSame(oldStock, line.useComposition);
            Assert.AreEqual((8, 0, 2, 60, DiagramType.LimittedExpressPrior),
                (oldStock.HeldUnits, newStock.HeldUnits, line.useCompositionNum, line.runningPerDay, line.diagram));
        }

        [TestMethod]
        public void CurrentBehaviorPartlyUnavailableReplacementReleasesOldDirectStockBeforeThrowing()
        {
            var game = Game();
            var line = Line(game);
            var oldStock = Stock(game);
            line.SettingComposition(oldStock, 60, DiagramType.LimittedExpressPrior, game);
            var newStock = Stock(game, 1);
            // 一部だけ足りない場合は事前チェックを通り、旧編成を返却した後で失敗する。
            Assert.ThrowsException<InvalidOperationException>(() => line.SettingComposition(newStock, 60, DiagramType.Regular, game));
            Assert.IsNull(line.useComposition);
            Assert.AreEqual((10, 1, 0, 0, DiagramType.LimittedExpressPrior),
                (oldStock.HeldUnits, newStock.HeldUnits, line.useCompositionNum, line.runningPerDay, line.diagram));
        }

        [TestMethod]
        public void CurrentBehaviorNoDiagramSelectionThrowsBeforeChangingService()
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            // Noneによる運行停止は、後続の不足編成数計算で例外になる現状。
            Assert.ThrowsException<ArgumentException>(() => line.SettingComposition(stock, 0, DiagramType.None, game));
            Assert.AreSame(stock, line.useComposition);
            Assert.AreEqual((8, 2, 60, DiagramType.LimittedExpressPrior),
                (stock.HeldUnits, line.useCompositionNum, line.runningPerDay, line.diagram));
        }

        [TestMethod]
        public void ThroughServiceChangesRouteDiagramAndReusesAvailableStock()
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game);
            through.SettingComposition(stock, 110, game);
            Assert.AreEqual((7, 3, 110, DiagramType.Regular),
                (stock.HeldUnits, through.useCompositionNum, through.runningPerDay, line.diagram));
            through.SettingComposition(stock, 60, game);
            Assert.AreEqual((8, 2, 60, DiagramType.Regular),
                (stock.HeldUnits, through.useCompositionNum, through.runningPerDay, line.diagram));
            through.DiagramReset();
            Assert.IsNull(through.useComposition);
            Assert.AreEqual((10, 0, 0, DiagramType.Regular),
                (stock.HeldUnits, through.useCompositionNum, through.runningPerDay, line.diagram));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RouteDiagramEvaluationRestoresOriginalDiagram(bool exceedsAllCapacity)
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var result = through.JudgeDiagramForRunningPerDay(exceedsAllCapacity ? 1000 : 110, game);
            Assert.AreEqual(!exceedsAllCapacity, result.isAcceptableFreq);
            Assert.AreEqual(DiagramType.LimittedExpressPrior, line.diagram);
            if (!exceedsAllCapacity) Assert.AreEqual(DiagramType.Regular, result.lineDiagramPairs[line]);
        }

        [TestMethod]
        public void IncompatibleThroughReplacementPreservesAllocationAndRouteDiagram()
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game);
            through.SettingComposition(stock, 60, game);
            var replacement = Stock(game);
            replacement.Gauge = CarGaugeEnum.Regular;
            Assert.ThrowsException<CompositionNotAppliedException>(() => through.SettingComposition(replacement, 110, game));
            Assert.AreSame(stock, through.useComposition);
            Assert.AreEqual((8, 10, 2, 60, DiagramType.LimittedExpressPrior),
                (stock.HeldUnits, replacement.HeldUnits, through.useCompositionNum, through.runningPerDay, line.diagram));
        }

        [TestMethod]
        public void MultipleRouteSectionsRestoreDiagramsAndApplyOnlyBottleneckChange()
        {
            var game = Game();
            var first = Line(game);
            var second = Line(game);
            second.Start = first.End;
            second.End = Town("C", 80);
            second.LaneNum = 1;
            var through = Through(game, first, second);
            var original = (first.diagram, second.diagram);
            var rejected = through.JudgeDiagramForRunningPerDay(1000, game);
            Assert.IsFalse(rejected.isAcceptableFreq);
            Assert.AreEqual(original, (first.diagram, second.diagram));
            var accepted = through.JudgeDiagramForRunningPerDay(35, game);
            Assert.IsTrue(accepted.isAcceptableFreq);
            Assert.AreEqual(original, (first.diagram, second.diagram));
            Assert.AreEqual((DiagramType.LimittedExpressPrior, DiagramType.Regular),
                (accepted.lineDiagramPairs[first], accepted.lineDiagramPairs[second]));
            var stock = Stock(game);
            through.SettingComposition(stock, 35, game);
            Assert.AreEqual((DiagramType.LimittedExpressPrior, DiagramType.Regular), (first.diagram, second.diagram));
            Assert.AreEqual((8, 2, 35), (stock.HeldUnits, through.useCompositionNum, through.runningPerDay));
            var incompatible = Stock(game);
            second.gauge = RailGaugeEnum.Regular;
            Assert.ThrowsException<CompositionNotAppliedException>(() => through.SettingComposition(incompatible, 50, game));
            Assert.AreEqual((DiagramType.LimittedExpressPrior, DiagramType.Regular), (first.diagram, second.diagram));
            Assert.AreSame(stock, through.useComposition);
            Assert.AreEqual((8, 10, 2, 35), (stock.HeldUnits, incompatible.HeldUnits, through.useCompositionNum, through.runningPerDay));
        }

        [TestMethod]
        public void CurrentBehaviorPartlyUnavailableThroughReplacementReleasesOldStockBeforeThrowing()
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var oldStock = Stock(game);
            through.SettingComposition(oldStock, 60, game);
            var replacement = Stock(game, 1);
            Assert.ThrowsException<InvalidOperationException>(() => through.SettingComposition(replacement, 60, game));
            Assert.IsNull(through.useComposition);
            Assert.AreEqual((10, 1, 0, 0, DiagramType.LimittedExpressPrior),
                (oldStock.HeldUnits, replacement.HeldUnits, through.useCompositionNum, through.runningPerDay, line.diagram));
        }
    }
}
