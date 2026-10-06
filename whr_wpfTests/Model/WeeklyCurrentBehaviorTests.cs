using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class WeeklyCurrentBehaviorTests
    {
        // 期待値は現在のWPF版の挙動。仕様として望ましいかは別途判断する。
        [DataTestMethod]
        [DataRow(1, 1, 1880, 1, 2)]
        [DataRow(1, 4, 1880, 2, 1)]
        [DataRow(12, 4, 1881, 1, 1)]
        public void CalendarAdvancesAtWeekMonthAndYearBoundaries(int month, int week, int yearAfter, int monthAfter, int weekAfter)
        {
            var game = Game();
            game.Month = month;
            game.Week = week;
            game.NextWeek();
            Assert.AreEqual((yearAfter, monthAfter, weekAfter), (game.Year, game.Month, game.Week));
            Assert.AreEqual(1000000L, game.Money);
            Assert.AreEqual((0, 0), (game.income, game.outlay));
            Assert.AreEqual(yearAfter == 1880 ? 500 : 506, game.Ap);
        }

        [TestMethod]
        public void WeeklyTotalsReplacePreviousWeekAndAllInvestmentsAccumulate()
        {
            var game = Game();
            game.income = 999;
            game.outlay = 888;
            game.weeklyInvestment = new InvestmentAmount
            {
                steam = InvestmentAmountEnum.MN2000, electricMotor = InvestmentAmountEnum.MN5000,
                diesel = InvestmentAmountEnum.OK1, linearMotor = InvestmentAmountLinearEnum.OK10,
                newPlan = InvestmentAmountEnum.OK5
            };
            game.AccumulatedInvest = default;
            game.NextWeek();
            Assert.AreEqual(983300L, game.Money);
            Assert.AreEqual((0, 16700), (game.income, game.outlay));
            Assert.AreEqual((200L, 500L, 1000L, 10000L, 5000L),
                (game.AccumulatedInvest.steam, game.AccumulatedInvest.electricMotor,
                game.AccumulatedInvest.diesel, game.AccumulatedInvest.linearMotor, game.AccumulatedInvest.newPlan));
        }

        [DataTestMethod]
        [DataRow(1879, 100, 1000000L, 0, 0)]
        [DataRow(1880, 100, 1000100L, 100, 0)]
        [DataRow(1881, 100, 1000100L, 100, 0)]
        [DataRow(1882, 100, 1000000L, 0, 0)]
        [DataRow(1880, -100, 999900L, 0, 100)]
        public void SubsidyIncludesBothBoundaryYearsAndNegativeAmounts(int year, int subsidy, long money, int income, int outlay)
        {
            var game = Game();
            game.Year = year;
            game.HojoStartYear = 1880;
            game.HojoEndYear = 1881;
            game.HojoAmount = subsidy;
            game.NextWeek();
            Assert.AreEqual((money, income, outlay), (game.Money, game.income, game.outlay));
        }

        [TestMethod]
        public void SubsidyAtYearEndUsesYearBeforeCalendarAdvance()
        {
            var game = Game();
            game.HojoStartYear = game.HojoEndYear = 1880;
            game.HojoAmount = 100;
            YearEnd(game);
            game.NextWeek();
            Assert.AreEqual(1881, game.Year);
            Assert.AreEqual(1000100L, game.Money);
        }

        [TestMethod]
        public void CurrentBehaviorSubsidyCanExceedMoneyCapAndInvestmentCanCreateDebt()
        {
            var game = Game();
            game.Money = 3000000000;
            game.HojoStartYear = game.HojoEndYear = 1880;
            game.HojoAmount = 100;
            game.NextWeek();
            Assert.AreEqual(2010000100L, game.Money);
            game.HojoAmount = 0;
            game.Money = 0;
            game.weeklyInvestment.steam = InvestmentAmountEnum.MN2000;
            game.NextWeek();
            Assert.AreEqual(-200L, game.Money);
            Assert.AreEqual(3, game.Week);
        }

        [DataTestMethod]
        [DataRow("steam", 40, 5000L, 40)]
        [DataRow("steam", 40, 5001L, 45)]
        [DataRow("steam", 145, 8000000L, 150)]
        [DataRow("electric", 0, 300000L, 0)]
        [DataRow("electric", 0, 300001L, 60)]
        [DataRow("electric", 355, 1000000000L, 370)]
        [DataRow("electric", 980, 1000000000L, 990)]
        [DataRow("diesel", 0, 300000L, 0)]
        [DataRow("diesel", 0, 300001L, 40)]
        [DataRow("diesel", 355, 1000000000L, 370)]
        [DataRow("linear", 0, 37500000L, 0)]
        [DataRow("linear", 0, 37500001L, 300)]
        [DataRow("linear", 980, 4000000000L, 990)]
        public void EngineDevelopmentRecordsStrictThresholdsAndSameWeekTransitions(string engine, int speed, long accumulated, int speedAfter)
        {
            var game = Game();
            game.AccumulatedInvest = default;
            switch (engine)
            {
                case "steam": game.genkaiJoki = speed; game.AccumulatedInvest.steam = accumulated; break;
                case "electric": game.genkaiDenki = speed; game.AccumulatedInvest.electricMotor = accumulated; break;
                case "diesel": game.genkaiKidosha = speed; game.AccumulatedInvest.diesel = accumulated; break;
                case "linear": game.genkaiLinear = speed; game.AccumulatedInvest.linearMotor = accumulated; break;
            }
            var messages = game.NextWeek();
            int actual = engine switch
            {
                "steam" => game.genkaiJoki, "electric" => game.genkaiDenki,
                "diesel" => game.genkaiKidosha, _ => game.genkaiLinear
            };
            Assert.AreEqual(speedAfter, actual);
            Assert.AreEqual(speed == speedAfter, messages.Count == 0);
        }

        [TestMethod]
        public void ReachingEngineInvestmentStopPointsDisablesWeeklyInvestment()
        {
            var game = Game();
            game.genkaiJoki = 145;
            game.genkaiDenki = 355;
            game.genkaiLinear = 980;
            game.weeklyInvestment.steam = InvestmentAmountEnum.MN2000;
            game.weeklyInvestment.electricMotor = InvestmentAmountEnum.MN2000;
            game.weeklyInvestment.linearMotor = InvestmentAmountLinearEnum.OK10;
            game.AccumulatedInvest.steam = 8000000;
            game.AccumulatedInvest.electricMotor = 250000000;
            game.AccumulatedInvest.linearMotor = 4000000000;
            game.NextWeek();
            // 電車は355→360で投資を停止し、同じ週の後続判定で370まで進む。
            Assert.AreEqual((150, 370, 990), (game.genkaiJoki, game.genkaiDenki, game.genkaiLinear));
            Assert.AreEqual((InvestmentAmountEnum.Nothing, InvestmentAmountEnum.Nothing, InvestmentAmountLinearEnum.Nothing),
                (game.weeklyInvestment.steam, game.weeklyInvestment.electricMotor, game.weeklyInvestment.linearMotor));
        }

        [DataTestMethod]
        [DataRow(99999L, 0, 0)]
        [DataRow(100000L, 1, 5)]
        [DataRow(500000L, 2, 5)]
        [DataRow(1000000L, 3, 5)]
        [DataRow(2000000L, 4, 5)]
        [DataRow(3000000L, 5, 5)]
        [DataRow(5000000L, 6, 5)]
        [DataRow(20000000L, 7, 5)]
        [DataRow(30000000L, 8, 5)]
        [DataRow(50000000L, 9, 5)]
        [DataRow(80000000L, 10, 10)]
        public void SpecialTechnologiesUnlockCumulativelyAndOnlyOnce(long accumulated, int count, int capacityBonus)
        {
            var game = Game();
            game.AccumulatedInvest.newPlan = accumulated;
            var messages = game.NextWeek();
            Assert.AreEqual(count, DevelopedCount(game));
            Assert.AreEqual((1 << count) - 1, DevelopedMask(game));
            Assert.AreEqual(capacityBonus, game.genkaikyoyo);
            Assert.AreEqual(count, messages.Count);
            Assert.AreEqual(0, game.NextWeek().Count);
            Assert.AreEqual(capacityBonus, game.genkaikyoyo);
        }

        [TestMethod]
        public void CompletingDynamicSignalStopsInvestmentAndAnnualPopulationSeesNewTechnology()
        {
            var game = Game();
            game.AccumulatedInvest.newPlan = 79999800;
            game.weeklyInvestment.newPlan = InvestmentAmountEnum.MN2000;
            YearEnd(game);
            game.NextWeek();
            Assert.AreEqual(InvestmentAmountEnum.Nothing, game.weeklyInvestment.newPlan);
            Assert.AreEqual(506, game.Ap);
            Assert.AreEqual(353, game.stations.Sum(town => town.Population));
        }

        [TestMethod]
        public void WarStartsAndEndsOnYearAdvance()
        {
            var game = Game();
            var war = new GameInfo.WarMode { StartYear = 1881, EndYear = 1882, kamotsuIndex = 120 };
            game.warModeList.Add(war);
            YearEnd(game);
            Assert.IsTrue(game.NextWeek().Single().Contains("戦時体制に突入"));
            Assert.AreSame(war, game.modss);
            YearEnd(game);
            Assert.IsTrue(game.NextWeek().Single().Contains("戦時体制は終了"));
            Assert.IsNull(game.modss);
        }

        [TestMethod]
        public void MoneyAndTechnologyGoalsCompleteTogetherAndClearAllGoals()
        {
            var game = Game();
            var mode = game.SelectedMode;
            mode.goalMoney = 1000000;
            mode.goalTechDevelop.Add(PowerEnum.Electricity, 60);
            Assert.AreEqual(0, game.NextWeek().Count);
            game.AccumulatedInvest.electricMotor = 300001;
            var messages = game.NextWeek();
            Assert.IsTrue(messages.Any(message => message.Contains("目標を達成")));
            Assert.AreEqual(2300, game.MYear);
            Assert.IsNull(mode.goalMoney);
            Assert.AreEqual(0, mode.goalTechDevelop.Count);
            Assert.IsNull(mode.goalLineMake);
            Assert.IsNull(mode.goalLineManage);
            Assert.IsNull(mode.goalLineBestSpeed.Item1);
            Assert.AreEqual(0, game.NextWeek().Count);
        }

        [TestMethod]
        public void ConstructionGoalRequiresEveryTargetLineAndResetsFreeModeDeadline()
        {
            var game = Game();
            var line = Line(game, false);
            game.SelectedMode.goalLineMake = LineGoalTargetEnum.All;
            game.MYear = 1920;
            game.NextWeek();
            Assert.IsNotNull(game.SelectedMode.goalLineMake);
            line.IsExist = true;
            Assert.IsTrue(game.NextWeek().Any(message => message.Contains("目標を達成")));
            Assert.AreEqual(2300, game.MYear);
        }

        [TestMethod]
        public void CurrentBehaviorDeadlineFailureOccursAfterWeekAndPopulationHaveChanged()
        {
            var game = Game();
            game.SelectedMode.MYear = 1880;
            game.MYear = 1880;
            game.SelectedMode.goalMoney = long.MaxValue;
            YearEnd(game);
            // 失敗しても巻き戻されない現状を記録する。望ましい仕様とはまだ判定しない。
            Assert.ThrowsException<GameOverException>(() => game.NextWeek());
            Assert.AreEqual((1881, 1, 1, 506), (game.Year, game.Month, game.Week, game.Ap));
            Assert.AreEqual(252, game.stations.Sum(town => town.Population));
        }
    }
}
