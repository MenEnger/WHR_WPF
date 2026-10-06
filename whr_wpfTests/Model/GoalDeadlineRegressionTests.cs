using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("GoalRegression")]
    public class GoalDeadlineRegressionTests
    {
        [TestMethod]
        public void Completed1880GoalsRemainInFreeModeAfterOriginalDeadline()
        {
            var game = Scenario1880();
            var line = Line(game);
            line.grade = LineGrade.MostImportant;
            // 回収した1880年モードと同じ敷設・蒸気100・期限1920の条件。
            var messages = game.NextWeek();
            Assert.AreEqual(1, messages.Count(message => message.Contains("目標を達成")));
            Assert.IsNull(game.SelectedMode.goalLineMake);
            Assert.AreEqual(0, game.SelectedMode.goalTechDevelop.Count);
            Assert.AreEqual((1920, 2300), (game.SelectedMode.MYear, game.MYear));

            game.Year = 1920;
            YearEnd(game);
            messages = game.NextWeek();
            Assert.AreEqual((1921, 1, 1), (game.Year, game.Month, game.Week));
            Assert.IsFalse(messages.Any(message => message.Contains("目標を達成")));
            Assert.IsFalse(game.NextWeek().Any(message => message.Contains("目標を達成")));
        }

        [TestMethod]
        public void Unfinished1880GoalsStillFailAfterDeadline()
        {
            var game = Scenario1880();
            var line = Line(game, false);
            line.grade = LineGrade.MostImportant;
            game.Year = 1920;
            YearEnd(game);
            Assert.ThrowsException<GameOverException>(() => game.NextWeek());
            Assert.AreEqual(1921, game.Year);
            Assert.AreEqual(1920, game.MYear);
            Assert.IsNotNull(game.SelectedMode.goalLineMake);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void NonPositiveDeadlineDoesNotEndAnUnfinishedGame(int deadline)
        {
            var game = Scenario1880();
            game.SelectedMode.MYear = deadline;
            game.MYear = deadline;
            var line = Line(game, false);
            line.grade = LineGrade.MostImportant;
            game.Year = 1920;
            YearEnd(game);
            game.NextWeek();
            Assert.AreEqual(1921, game.Year);
            Assert.IsNotNull(game.SelectedMode.goalLineMake);
        }

        [TestMethod]
        public void GoalsCanCompleteWhenUpdatedDateIsStillWithinDeadlineYear()
        {
            var game = Scenario1880();
            var line = Line(game);
            line.grade = LineGrade.MostImportant;
            game.Year = 1920;
            game.Month = 12;
            game.Week = 3;
            Assert.IsTrue(game.NextWeek().Any(message => message.Contains("目標を達成")));
            Assert.AreEqual((1920, 12, 4, 2300), (game.Year, game.Month, game.Week, game.MYear));
        }

        [TestMethod]
        public void ExistingDeadlinePriorityIsPreservedOnFinalWeekRollover()
        {
            var game = Scenario1880();
            var line = Line(game);
            line.grade = LineGrade.MostImportant;
            game.Year = 1920;
            YearEnd(game);
            // 日付更新後の期限判定が達成より先という現状は、今回の修正では変えない。
            Assert.ThrowsException<GameOverException>(() => game.NextWeek());
            Assert.IsNotNull(game.SelectedMode.goalLineMake);
        }

        [TestMethod]
        public void FreeModeStillHonorsItsExtendedEndYear()
        {
            var game = Scenario1880();
            var line = Line(game);
            line.grade = LineGrade.MostImportant;
            game.NextWeek();
            game.Year = game.MYear;
            game.Month = 12;
            game.Week = 3;
            game.NextWeek();
            Assert.AreEqual(2300, game.Year);
            Assert.ThrowsException<GameOverException>(() => game.NextWeek());
            Assert.AreEqual(2301, game.Year);
        }

        private static GameInfo Scenario1880()
        {
            var game = Game();
            var mode = game.SelectedMode;
            mode.Year = 1919;
            mode.MYear = 1920;
            mode.genkaiJoki = 100;
            mode.goalLineMake = LineGoalTargetEnum.MostImportant;
            mode.goalTechDevelop.Add(PowerEnum.Steam, 100);
            // モード適用時に実行中の期限もコピーされる通常の開始経路を使う。
            game.SelectedMode = mode;
            return game;
        }
    }
}
