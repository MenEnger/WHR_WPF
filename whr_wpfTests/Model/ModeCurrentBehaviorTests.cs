using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class ModeCurrentBehaviorTests
    {
        [DataTestMethod]
        [DataRow(30, DiagramType.LimittedExpressPrior, 1)]
        [DataRow(70, DiagramType.Regular, 2)]
        [DataRow(85, DiagramType.OverCrowded, 3)]
        [DataRow(100, DiagramType.Parallel, 4)]
        public void NonemptyModeInitializesInfrastructureServicesTechnologyAndPopulation(int trips, DiagramType diagram, int units)
        {
            var game = Game();
            var line = Line(game, false);
            var through = Through(game, line);
            var stock = Stock(game);
            game.compositions.Clear();
            var mode = Mode(stock, trips);
            game.SelectedMode = mode;
            Assert.AreSame(mode, game.SelectedMode);
            Assert.AreEqual((1920, 1960, 200000L, 925), (game.Year, game.MYear, game.Money, game.Ap));
            CollectionAssert.AreEqual(new[] { 200, 300 }, game.stations.Select(town => town.Population).ToArray());
            Assert.AreEqual((100, 60, true, 5), (game.genkaiJoki, game.genkaiDenki, game.isDevelopedBlockingSignal, game.genkaikyoyo));
            Assert.AreEqual((true, RailTypeEnum.Iron, (bool?)false, RailGaugeEnum.Narrow, 60, 2, 12345, diagram),
                (line.IsExist, line.Type, line.IsElectrified, line.gauge, line.bestSpeed, line.LaneNum, line.retentionRate, line.diagram));
            Assert.AreSame(stock, line.useComposition);
            Assert.AreSame(stock, through.useComposition);
            Assert.AreEqual((units, trips, units, trips), (line.useCompositionNum, line.runningPerDay, through.useCompositionNum, through.runningPerDay));
            Assert.AreSame(stock, game.compositions.Single());
            // 現状のモード適用は投入数を設定するが、余剰編成数からは差し引かない。
            Assert.AreEqual(10, stock.HeldUnits);
        }

        [TestMethod]
        public void CurrentBehaviorInvalidModeLeavesPartiallyAppliedSettings()
        {
            var game = Game();
            var line = Line(game, false);
            var through = Through(game, line);
            var stock = Stock(game);
            game.compositions.Clear();
            var mode = Mode(stock, 1000);
            Assert.ThrowsException<CannotContinueException>(() => game.SelectedMode = mode);
            // エラー時にも選択モード・日時・資金・人口・運行設定は途中まで変更される。
            Assert.AreSame(mode, game.SelectedMode);
            Assert.AreEqual((1920, 1960, 200000L, 500), (game.Year, game.MYear, game.Money, game.Ap));
            CollectionAssert.AreEqual(new[] { 200, 300 }, game.stations.Select(town => town.Population).ToArray());
            Assert.AreEqual((true, 1000, 1000, 0, 0), (line.IsExist, line.runningPerDay, through.runningPerDay, line.useCompositionNum, through.useCompositionNum));
            Assert.AreSame(stock, line.useComposition);
            Assert.AreSame(stock, through.useComposition);
            Assert.AreEqual(0, game.compositions.Count);
            Assert.AreEqual(10, stock.HeldUnits);
        }

        private static Mode Mode(DefautltComposition stock, int trips) => new Mode
        {
            Year = 1920, MYear = 1960, Money = 200000, peopleNume = 2, peopleDenom = 1,
            genkaiDenki = 60, isDevelopedBlockingSignal = true,
            DefautltCompositions = new List<DefautltComposition> { stock },
            LineSettings = new List<Mode.LineDefaultSetting>
            {
                new Mode.LineDefaultSetting
                {
                    IsExist = true, Type = RailTypeEnum.Iron, IsElectrified = false,
                    gauge = RailGaugeEnum.Narrow, bestSpeed = 60, LaneNum = 2,
                    retentionRate = 12345, useComposition = stock, runningPerDay = trips
                }
            },
            KeitoDefaultSettings = new List<Mode.KeitoDefaultSetting>
            {
                new Mode.KeitoDefaultSetting { useComposition = stock, runningPerDay = trips }
            }
        };
    }
}
