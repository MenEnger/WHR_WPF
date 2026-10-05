using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using whr_wpf.Model;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("OriginalPopulation")]
    public class GameInfoTests
    {
        // 原作1.52の整数計算から求めた期待値。Apは人口比率を掛ける前の総人口。
        private const int BasePopulation = 200000;
        private static Station Town(string name, int population, StationSize size = StationSize.Other)
            => new Station { Name = name, Population = population, Size = size };
        private static DefautltComposition Composition(int speed = 60)
            => new DefautltComposition { BestSpeed = speed, Tilt = CarTiltEnum.None, CarCount = 8 };
        private static Line Connect(Station start, Station end, int distance = 9)
        {
            var line = new Line
            {
                IsExist = true, Start = start, End = end, Distance = distance,
                bestSpeed = 60, LaneNum = 2, diagram = DiagramType.LimittedExpressPrior,
                useComposition = Composition(), useCompositionNum = 1, runningPerDay = 1
            };
            start.BelongingLines.Add(line);
            end.BelongingLines.Add(line);
            return line;
        }
        private static Longway Path(Station start, Station end, params Line[] route)
            => new Longway { start = start, end = end, route = route.ToList() };
        private static Mode EmptyMode() => new Mode
        {
            Year = 1880, Money = 1000000, goalMoney = long.MaxValue,
            DefautltCompositions = new List<DefautltComposition>(),
            LineSettings = new List<Mode.LineDefaultSetting>(),
            KeitoDefaultSettings = new List<Mode.KeitoDefaultSetting>()
        };
        private static GameInfo Game(params Station[] towns)
        {
            var game = new GameInfo
            {
                stations = towns.ToList(), longwayList = new List<Longway>(),
                warModeList = new List<GameInfo.WarMode>(), TechCost = 100, BasicYear = 1880,
                Difficulty = DifficultyLevelEnum.Normal
            };
            game.SelectedMode = EmptyMode();
            return game;
        }
        private static void AdvanceYear(GameInfo game)
        {
            game.Month = 12;
            game.Week = 4;
            game.NextWeek();
        }
        private static Dictionary<Station, int> Distribute(Station[] towns, params Longway[] paths)
            => new GameInfo().CalculateStationPopulationDistribution(towns, paths, BasePopulation);

        [TestMethod]
        public void SingleTownApIncludesPopulationRatio()
        {
            var town = Town("単独都市", 50);
            Assert.AreEqual(100000, Distribute(new[] { town })[town]);
        }
        [TestMethod]
        public void DisconnectedTownsKeepRelativeSharesWithoutChangingInput()
        {
            var a = Town("A", 50);
            var b = Town("B", 100);
            var result = Distribute(new[] { a, b });
            Assert.AreEqual(33333, result[a]);
            Assert.AreEqual(66666, result[b]);
            Assert.AreEqual(50, a.Population);
        }
        [DataTestMethod]
        [DataRow(20)]
        [DataRow(17)]
        public void CapitalBonusExcludesTransferTown(int capitalSize)
        {
            var a = Town("首都", 50, (StationSize)capitalSize);
            var b = Town("乗換都市", 50, (StationSize)16);
            var result = Distribute(new[] { a, b });
            Assert.AreEqual(50248, result[a]);
            Assert.AreEqual(49751, result[b]);
        }
        [TestMethod]
        public void DirectAndLongwaySharesBothApply()
        {
            var a = Town("A", 50);
            var b = Town("B", 100);
            var line = Connect(a, b);
            Assert.AreEqual(10, line.CalcAverageRequireMinutes());
            var result = Distribute(new[] { a, b }, Path(a, b, line));
            Assert.AreEqual(33793, result[a]);
            Assert.AreEqual(66206, result[b]);
        }
        [TestMethod]
        public void ConnectedSharesPreserveIntegerMultiplicationOrder()
        {
            var a = Town("A", 51);
            var b = Town("B", 79);
            var line = Connect(a, b, 20);
            Assert.AreEqual(22, line.CalcAverageRequireMinutes());
            var result = Distribute(new[] { a, b }, Path(a, b, line));
            // 3*79/10=23と3*51/10=15を、直接接続と長距離接続でそれぞれ加算。
            Assert.AreEqual(39354, result[a]);
            Assert.AreEqual(60645, result[b]);
        }
        [DataTestMethod]
        [DataRow(false, 1, 1)]
        [DataRow(true, 0, 0)]
        [DataRow(true, 0, 10)]
        public void UnbuiltOrUnallocatedLineAddsNoShares(bool exists, int units, int trips)
        {
            var a = Town("A", 50);
            var b = Town("B", 100);
            var line = Connect(a, b);
            line.IsExist = exists;
            line.useCompositionNum = units;
            line.runningPerDay = trips;
            var result = Distribute(new[] { a, b }, Path(a, b, line));
            Assert.AreEqual(33333, result[a]);
            Assert.AreEqual(66666, result[b]);
        }
        [TestMethod]
        public void ThroughOnlyServiceAddsLongwayShareWithoutDirectShare()
        {
            var a = Town("A", 50);
            var b = Town("B", 100);
            var line = Connect(a, b);
            line.useCompositionNum = 0;
            line.runningPerDay = 0;
            line.belongingKeitoDiagrams.Add(new KeitoDiagram
            {
                route = new List<Line> { line }, useComposition = Composition(),
                useCompositionNum = 1, runningPerDay = 1
            });
            Assert.AreEqual(54, line.CalcHyokaSpeed());
            var result = Distribute(new[] { a, b }, Path(a, b, line));
            Assert.AreEqual(33565, result[a]);
            Assert.AreEqual(66434, result[b]);
        }
        [TestMethod]
        public void EvaluationSpeedAveragesAllocatedServicesWithoutTripWeights()
        {
            var line = Connect(Town("A", 50), Town("B", 100));
            line.bestSpeed = 120;
            line.belongingKeitoDiagrams.Add(new KeitoDiagram
            {
                route = new List<Line> { line }, useComposition = Composition(120),
                useCompositionNum = 1, runningPerDay = 10
            });
            line.belongingKeitoDiagrams.Add(new KeitoDiagram
            {
                route = new List<Line> { line }, useComposition = Composition(30),
                useCompositionNum = 0, runningPerDay = 0
            });
            Assert.AreEqual(81, line.CalcHyokaSpeed());
            Assert.AreEqual(6, line.CalcAverageRequireMinutes());
        }
        [TestMethod]
        public void LongwayRequiresEverySegmentToOperate()
        {
            var a = Town("A", 50);
            var middle = Town("中間", 50);
            var b = Town("B", 100);
            var first = Connect(a, middle);
            var last = Connect(middle, b);
            last.IsExist = false;
            var towns = new[] { a, middle, b };
            var expected = Distribute(towns);
            var actual = Distribute(towns, Path(a, b, first, last), Path(a, b));
            foreach (var town in towns) Assert.AreEqual(expected[town], actual[town]);
        }
        [TestMethod]
        public void EngineTechnologyBonusesRespectThresholdsAndCombinedSpeedBonus()
        {
            var town = Town("A", 50);
            var game = new GameInfo
            {
                genkaiDenki = 200, genkaiKidosha = 40, genkaiLinear = 300,
                isDevelopedBlockingSignal = true
            };
            int Population() => game.CalculateStationPopulationDistribution(new[] { town }, new Longway[0], 1000)[town];
            Assert.AreEqual(590, Population());
            game.genkaiDenki = 201;
            Assert.AreEqual(620, Population());
            game.genkaiKidosha = 201;
            Assert.AreEqual(620, Population());
            game.genkaiLinear = 990;
            Assert.AreEqual(720, Population());
        }
        [TestMethod]
        public void EverySpecialTechnologyAddsTwoPercentagePoints()
        {
            var town = Town("A", 50);
            var game = new GameInfo
            {
                isDevelopedDynamicSignal = true, isDevelopedFreeGauge = true,
                isDevelopedMachineTilt = true, isDevelopedDualSeat = true,
                isDevelopedRetructableLong = true, isDevelopedRichCross = true,
                isDevelopedCarTiltPendulum = true, isDevelopedAutoGate = true,
                isDevelopedConvertibleCross = true, isDevelopedBlockingSignal = true
            };
            Assert.AreEqual(700, game.CalculateStationPopulationDistribution(new[] { town }, new Longway[0], 1000)[town]);
        }
        [TestMethod]
        public void ModePopulationAndTechnologyApplyBeforeApInitialization()
        {
            var town = Town("A", 50);
            var game = Game(town);
            var mode = EmptyMode();
            mode.peopleNume = 8;
            mode.peopleDenom = 5;
            mode.genkaiDenki = 60;
            game.SelectedMode = mode;
            Assert.AreEqual(80, town.Population);
            Assert.AreEqual(153, game.Ap);
            game.MYear = 2300;
            AdvanceYear(game);
            Assert.AreEqual(154, game.Ap);
            Assert.AreEqual(80, town.Population);
        }
        [TestMethod]
        public void AnnualRoundingDoesNotRebaseApFromVisiblePopulation()
        {
            var a = Town("A", 50);
            var b = Town("B", 100);
            var game = Game(a, b);
            game.MYear = 2300;
            AdvanceYear(game);
            Assert.AreEqual(303, game.Ap);
            Assert.AreEqual(50, a.Population);
            Assert.AreEqual(101, b.Population);
            AdvanceYear(game);
            Assert.AreEqual(306, game.Ap);
            Assert.AreEqual(50, a.Population);
            Assert.AreEqual(102, b.Population);
        }
        [DataTestMethod]
        [DataRow(DifficultyLevelEnum.VeryEasy, 1980, 1000, 2024)]
        [DataRow(DifficultyLevelEnum.Easy, 1980, 1000, 2010)]
        [DataRow(DifficultyLevelEnum.Normal, 1980, 1000, 2000)]
        [DataRow(DifficultyLevelEnum.Hard, 1980, 1000, 2000)]
        [DataRow(DifficultyLevelEnum.VeryHard, 1980, 1000, 2000)]
        [DataRow(DifficultyLevelEnum.Normal, 1980, 2300, 2024)]
        [DataRow(DifficultyLevelEnum.Easy, 1098, 1000, 2024)]
        [DataRow(DifficultyLevelEnum.Easy, 1099, 1000, 2010)]
        public void AnnualGrowthUsesDifficultyAndModeDeadline(DifficultyLevelEnum difficulty, int year, int deadline, int expectedAp)
        {
            var game = Game(Town("A", 1000));
            game.Difficulty = difficulty;
            game.Year = year;
            game.MYear = deadline;
            AdvanceYear(game);
            Assert.AreEqual(year + 1, game.Year);
            Assert.AreEqual(expectedAp, game.Ap);
        }
        [TestMethod]
        public void TechnologyDevelopmentChangesVisiblePopulationWithoutRebasingAp()
        {
            var town = Town("A", 1000);
            var game = Game(town);
            game.Year = 1980;
            game.MYear = 1000;
            game.genkaiDenki = 60;
            AdvanceYear(game);
            Assert.AreEqual(2000, game.Ap);
            Assert.AreEqual(1040, town.Population);
        }
        [TestMethod]
        public void ZeroPopulationHasMinimumOneAndEmptyMapIsAllowed()
        {
            var town = Town("A", 0);
            var game = new GameInfo();
            Assert.AreEqual(1, game.CalculateStationPopulationDistribution(new[] { town }, new Longway[0], 0)[town]);
            Assert.AreEqual(0, Distribute(new Station[0]).Count);
        }
        [TestMethod]
        public void CapitalBonusTruncatesBeforeMultiplication()
        {
            var a = Town("首都", 51, StationSize.Capital);
            var b = Town("B", 79);
            Connect(a, b, 20);
            var result = Distribute(new[] { a, b });
            // 首都シェア5123を5123/10*101/10=5171に補正する。
            Assert.AreEqual(39515, result[a]);
            Assert.AreEqual(60484, result[b]);
        }
        [TestMethod]
        public void ShareHalvingKeepsIndependentlyRoundedTotal()
        {
            var a = Town("A", 499);
            var b = Town("B", 499);
            Connect(a, b, 20);
            var result = Distribute(new[] { a, b });
            // シェアは50049ずつ。合計100098/2=50049、各都市は25024。
            Assert.AreEqual(49999, result[a]);
            Assert.AreEqual(49999, result[b]);
        }
        [TestMethod]
        public void UnallocatedThroughTripsCannotMakeLongwayOperational()
        {
            var a = Town("A", 50);
            var b = Town("B", 100);
            var line = Connect(a, b);
            line.runningPerDay = 0;
            line.belongingKeitoDiagrams.Add(new KeitoDiagram
            {
                route = new List<Line> { line }, useComposition = Composition(),
                useCompositionNum = 0, runningPerDay = 10
            });
            var expected = Distribute(new[] { a, b });
            var actual = Distribute(new[] { a, b }, Path(a, b, line));
            Assert.AreEqual(expected[a], actual[a]);
            Assert.AreEqual(expected[b], actual[b]);
        }
        [TestMethod]
        public void LargeBaselineUsesWideIntermediateArithmetic()
        {
            var town = Town("A", 1000);
            Assert.AreEqual(500000000, new GameInfo().CalculateStationPopulationDistribution(new[] { town }, new Longway[0], 1000000000)[town]);
        }
    }
}
