using static whr_wpf.Model.Tests.WeeklyEventFixture;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class WeeklyNotificationBehaviorTests
    {
        [TestMethod]
        public void SimultaneousDevelopmentAnnualAndGoalMessagesKeepTheirOrderAndIntermediateValues()
        {
            var game = SimultaneousEvents();
            var messages = game.NextWeek();
            CollectionAssert.AreEqual(ExpectedEvents, messages);
            Assert.AreEqual((45, 370, 370, 310, 10),
                (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, game.genkaikyoyo));
            Assert.AreEqual((1881, 1, 1, 2300), (game.Year, game.Month, game.Week, game.MYear));
        }

        [TestMethod]
        public void DeadlineFailureReturnsNoMessagesButRetainsEarlierChanges()
        {
            var game = SimultaneousEvents();
            game.MYear = 1880;
            game.SelectedMode.goalMoney = long.MaxValue;
            game.weeklyInvestment.steam = InvestmentAmountEnum.MN2000;
            List<GameEvent>? messages = null;
            Assert.ThrowsException<GameOverException>(() => messages = game.NextWeek());
            Assert.IsNull(messages);
            Assert.AreEqual((45, 370, 370, 310, 10),
                (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, game.genkaikyoyo));
            Assert.AreEqual((1881, 1, 1, 1880), (game.Year, game.Month, game.Week, game.MYear));
            Assert.AreEqual(999800L, game.Money);
            Assert.IsNotNull(game.modss);
            Assert.AreEqual(long.MaxValue, game.SelectedMode.goalMoney);
        }

        [DataTestMethod]
        [DataRow(PowerEnum.Steam, 40, 5001L, EngineDevelopmentKind.SpeedImproved, 45)]
        [DataRow(PowerEnum.Electricity, 0, 300001L, EngineDevelopmentKind.Available, 60)]
        [DataRow(PowerEnum.Electricity, 980, 1000000000L, EngineDevelopmentKind.CostReduced, 990)]
        [DataRow(PowerEnum.Diesel, 0, 300001L, EngineDevelopmentKind.Available, 40)]
        [DataRow(PowerEnum.Diesel, 370, 1000000000L, EngineDevelopmentKind.SpeedImproved, 375)]
        [DataRow(PowerEnum.LinearMotor, 0, 37500001L, EngineDevelopmentKind.Available, 300)]
        [DataRow(PowerEnum.LinearMotor, 300, 4000000000L, EngineDevelopmentKind.SpeedImproved, 310)]
        [DataRow(PowerEnum.LinearMotor, 980, 4000000000L, EngineDevelopmentKind.SpeedImproved, 990)]
        public void EngineEventsRecordTheExecutedBranchAndBeforeAfterLevels(PowerEnum power, int before,
            long investment, EngineDevelopmentKind kind, int after)
        {
            var game = Game();
            game.AccumulatedInvest = default;
            switch (power)
            {
                case PowerEnum.Steam: game.genkaiJoki = before; game.AccumulatedInvest.steam = investment; break;
                case PowerEnum.Electricity: game.genkaiDenki = before; game.AccumulatedInvest.electricMotor = investment; break;
                case PowerEnum.Diesel: game.genkaiKidosha = before; game.AccumulatedInvest.diesel = investment; break;
                case PowerEnum.LinearMotor: game.genkaiLinear = before; game.AccumulatedInvest.linearMotor = investment; break;
            }
            var notification = game.NextWeek().OfType<EngineDevelopedEvent>().First();
            Assert.AreEqual(new EngineDevelopedEvent(power, kind, before, after), notification);
        }

        [TestMethod]
        public void ReturnedEventsRemainSnapshotsAfterFurtherDevelopmentAndModelMutation()
        {
            var game = SimultaneousEvents();
            var notifications = game.NextWeek();
            var original = notifications.ToArray();
            game.NextWeek();
            game.genkaiJoki = 999;
            game.genkaikyoyo = -1;
            game.MYear = -1;
            game.warModeList[0].StartYear = 9999;
            game.warModeList[0].EndYear = 9999;
            game.warModeList[0].kamotsuIndex = -1;
            CollectionAssert.AreEqual(ExpectedEvents, notifications);
            CollectionAssert.AreEqual(original, notifications);
        }

        [TestMethod]
        public void DieselAboveTheSpeedThresholdStillRunsSpeedThenCostBranches()
        {
            var game = Game();
            game.genkaiKidosha = 370;
            game.AccumulatedInvest.diesel = 1000000000;
            CollectionAssert.AreEqual(new GameEvent[]
            {
                new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.SpeedImproved, 370, 375),
                new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.CostReduced, 375, 385)
            }, game.NextWeek());
            Assert.AreEqual(385, game.genkaiKidosha);
        }

        [TestMethod]
        public void WarEndEventCopiesTheSettingBeforeItIsCleared()
        {
            var game = Game();
            var war = new GameInfo.WarMode { StartYear = 1881, EndYear = 1882, kamotsuIndex = 120 };
            game.warModeList.Add(war);
            YearEnd(game);
            var start = game.NextWeek().Single();
            YearEnd(game);
            var end = game.NextWeek().Single();
            Assert.IsNull(game.modss);
            war.StartYear = war.EndYear = 9999;
            war.kamotsuIndex = -1;
            Assert.AreEqual(new WarStartedEvent(1881, 1882, 120), start);
            Assert.AreEqual(new WarEndedEvent(1881, 1882, 120), end);
        }

        [TestMethod]
        public void GoalEventUsesTheRuntimeDeadlineAndActualFreeModeExtension()
        {
            var game = Game();
            game.SelectedMode.MYear = 1920;
            game.MYear = 1940;
            game.SelectedMode.goalMoney = 1;
            var notification = game.NextWeek().OfType<GoalsAchievedEvent>().Single();
            Assert.AreEqual(new GoalsAchievedEvent(1940, 2300), notification);
            Assert.AreEqual(1920, game.SelectedMode.MYear);
            Assert.AreEqual(2300, game.MYear);
        }

        [TestMethod]
        public void FourWeekFailureReturnsNoAggregateButRetainsSuccessfulEarlierWeeks()
        {
            var game = Game();
            game.Month = 12;
            game.Week = 2;
            game.MYear = 1880;
            game.SelectedMode.goalMoney = long.MaxValue;
            game.weeklyInvestment.steam = InvestmentAmountEnum.MN2000;
            game.AccumulatedInvest.steam = 5000;
            List<GameEvent>? notifications = null;
            var successfulWeeks = 0;
            var observed = new List<GameEvent>();
            Assert.ThrowsException<GameOverException>(() =>
            {
                for (int i = 0; i < 4; i++)
                {
                    observed.AddRange(game.NextWeek());
                    successfulWeeks++;
                }
                notifications = observed;
            });
            Assert.IsNull(notifications);
            Assert.AreEqual(2, successfulWeeks);
            Assert.AreEqual(new EngineDevelopedEvent(PowerEnum.Steam, EngineDevelopmentKind.SpeedImproved, 40, 45), observed.Single());
            Assert.AreEqual((1881, 1, 1), (game.Year, game.Month, game.Week));
            Assert.AreEqual(999400L, game.Money);
            Assert.AreEqual(45, game.genkaiJoki);
        }


    }
}
