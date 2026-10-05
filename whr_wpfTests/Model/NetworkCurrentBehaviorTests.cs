using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class NetworkCurrentBehaviorTests
    {
        [DataTestMethod]
        [DataRow(1, "1880/1/2;997970,68,98;660:100,150,80;17;502,10002,-11;417,10002,-19;1,10,1,10,1,10")]
        [DataRow(4, "1880/2/1;997874,66,98;660:100,150,80;17;491,10008,-50;408,10008,-76;1,10,1,10,1,10")]
        [DataRow(48, "1881/1/1;996459,58,90;667:101,150,81;17;430,10096,-631;357,10096,-910;1,10,1,10,1,10")]
        public void PassengerNetworkRecordsWeeklyMonthlyAndAnnualState(int weeks, string expected)
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.Nothing;
            var first = Line(game);
            var second = Line(game);
            second.Start.BelongingLines.Remove(second);
            second.End.BelongingLines.Remove(second);
            second.Start = first.End;
            second.End = Town("C", 80);
            game.stations.Add(second.End);
            second.Start.BelongingLines.Add(second);
            second.End.BelongingLines.Add(second);
            game.Ap = 660;
            var stock = Stock(game, 20);
            first.SettingComposition(stock, 10, DiagramType.LimittedExpressPrior, game);
            second.SettingComposition(stock, 10, DiagramType.LimittedExpressPrior, game);
            var through = Through(game, first, second);
            through.SettingComposition(stock, 10, game);
            game.longwayList.Add(new Longway { start = first.Start, end = second.End, route = new List<Line> { first, second } });
            for (int i = 0; i < weeks; i++) game.NextWeek();
            // 日時／資金・週収支／総人口・都市人口／余剰編成／各路線の旅客・定着率・累計収支／投入編成と本数。
            string snapshot = $"{game.Year}/{game.Month}/{game.Week};{game.Money},{game.income},{game.outlay};{game.Ap}:{string.Join(",", game.stations.Select(town => town.Population))};{stock.HeldUnits};{first.passengersLastWeek},{first.retentionRate},{first.totalBalance};{second.passengersLastWeek},{second.retentionRate},{second.totalBalance};{first.useCompositionNum},{first.runningPerDay},{second.useCompositionNum},{second.runningPerDay},{through.useCompositionNum},{through.runningPerDay}";
            Assert.AreEqual(expected, snapshot);
        }

        [DataTestMethod]
        [DataRow(false, 10, 10, 90, true)]
        [DataRow(true, 10, 26, 74, false)]
        [DataRow(true, 50, 362, -262, false)]
        public void CurrentBehaviorFreightCapacityChangesDifferBetweenPeaceAndWar(bool wartime, int freightSize, int freightAfter, int tripsAfter, bool overCapacity)
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = freightSize;
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 90, DiagramType.LimittedExpressPrior, game);
            game.longwayList.Add(new Longway { start = line.Start, end = line.End, route = new List<Line> { line } });
            if (wartime) game.modss = new GameInfo.WarMode { kamotsuIndex = 120, EndYear = 1885 };
            // 現状では、戦時に路線運行を削り過ぎると本数が負になる場合もある。
            game.NextWeek();
            Assert.AreEqual((freightAfter, tripsAfter, overCapacity, 2, 8),
                (line.kamotsuNumLastWeek, line.runningPerDay, line.isOverCapacity, line.useCompositionNum, stock.HeldUnits));
        }

        [TestMethod]
        public void WartimeFreightReducesThroughServiceAndClampsFreightAtCapacity()
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = 50;
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game);
            through.SettingComposition(stock, 90, game);
            game.longwayList.Add(new Longway { start = line.Start, end = line.End, route = new List<Line> { line } });
            game.modss = new GameInfo.WarMode { kamotsuIndex = 120, EndYear = 1885 };
            game.NextWeek();
            Assert.AreEqual((100, 0, true, 0, 2, 8),
                (line.kamotsuNumLastWeek, line.runningPerDay, line.isOverCapacity,
                through.runningPerDay, through.useCompositionNum, stock.HeldUnits));
        }

        [TestMethod]
        public void FreightIncomeStaysWithinRandomUnitPriceRangeAndAccountsBalance()
        {
            var game = Game();
            var line = Line(game);
            line.Distance = 1500;
            line.kamotsuNumLastWeek = 1;
            long money = game.Money;
            game.NextWeek();
            Assert.AreEqual(1, line.kamotsuNumLastWeek);
            Assert.IsTrue(line.incomeLastWeek >= 245 && line.incomeLastWeek <= 308);
            Assert.AreEqual(line.incomeLastWeek, (long)game.income);
            Assert.AreEqual(line.outlayLastWeek, (long)game.outlay);
            Assert.AreEqual(money + game.income - game.outlay, game.Money);
            Assert.AreEqual((long)game.income - game.outlay, line.totalBalance);
        }

        [TestMethod]
        public void FreightCarriesBetweenWeeksWithoutExceedingPeaceCapacity()
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = 10;
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 10, DiagramType.LimittedExpressPrior, game);
            game.longwayList.Add(new Longway { start = line.Start, end = line.End, route = new List<Line> { line } });
            for (int week = 0; week < 2; week++)
            {
                long money = game.Money;
                long balance = line.totalBalance;
                game.NextWeek();
                Assert.AreEqual((week == 0 ? 22 : 43, 10, false, 1, 9),
                    (line.kamotsuNumLastWeek, line.runningPerDay, line.isOverCapacity, line.useCompositionNum, stock.HeldUnits));
                Assert.AreEqual(money + game.income - game.outlay, game.Money);
                Assert.AreEqual(balance + game.income - game.outlay, line.totalBalance);
            }
        }

        [TestMethod]
        public void CurrentBehaviorNegativeWartimeTripsAreClampedOnFollowingWeek()
        {
            var game = Game();
            game.Kamotu = KamotsuEnum.EverIncrease;
            foreach (var town in game.stations) town.KamotsuKibo = 50;
            var line = Line(game);
            var stock = Stock(game);
            line.SettingComposition(stock, 90, DiagramType.LimittedExpressPrior, game);
            game.longwayList.Add(new Longway { start = line.Start, end = line.End, route = new List<Line> { line } });
            game.modss = new GameInfo.WarMode { kamotsuIndex = 120, EndYear = 1885 };
            game.NextWeek();
            Assert.AreEqual((362, -262, false), (line.kamotsuNumLastWeek, line.runningPerDay, line.isOverCapacity));
            game.NextWeek();
            Assert.AreEqual((100, 0, true, 2, 8),
                (line.kamotsuNumLastWeek, line.runningPerDay, line.isOverCapacity, line.useCompositionNum, stock.HeldUnits));
        }

        [TestMethod]
        public void CurrentBehaviorUnoperatedThroughDiagramResetThrows()
        {
            var game = Game();
            var through = Through(game, Line(game));
            // 編成未設定の系統はリセットできない現状を記録する。
            Assert.ThrowsException<NullReferenceException>(() => through.DiagramReset());
            Assert.AreEqual((0, 0), (through.useCompositionNum, through.runningPerDay));
        }
    }
}
