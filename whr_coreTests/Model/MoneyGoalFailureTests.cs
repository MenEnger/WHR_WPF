using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class MoneyGoalFailureTests
    {
        [TestMethod]
        public void InsufficientPaymentLeavesStateAndEmitsNoMoneyNotification()
        {
            var game = Game();
            game.Money = 99;
            var notifications = new List<(string? Name, long Money)>();
            game.PropertyChanged += (_, args) => notifications.Add((args.PropertyName, game.Money));
            var exception = Assert.ThrowsException<MoneyShortException>(() => game.SpendMoney(100));
            Assert.AreEqual(new MoneyShortageFailure(100, 99), exception.Failure);
            Assert.IsInstanceOfType<InvalidOperationException>(exception);
            Assert.AreEqual((99L, 0, 0), (game.Money, game.income, game.outlay));
            Assert.AreEqual(0, notifications.Count);
        }

        [DataTestMethod]
        [DataRow(100L, 100L, 0L)]
        [DataRow(101L, 100L, 1L)]
        [DataRow(100L, 0L, 100L)]
        public void AcceptedPaymentSubtractsAmountAndNotifiesExactlyOnce(long initial, long amount, long expected)
        {
            var game = Game();
            game.Money = initial;
            var notifications = new List<(string? Name, long Money)>();
            game.PropertyChanged += (_, args) => notifications.Add((args.PropertyName, game.Money));
            game.SpendMoney(amount);
            Assert.AreEqual((expected, 0, 0), (game.Money, game.income, game.outlay));
            CollectionAssert.AreEqual(new[] { ("Money", expected) }, notifications);
        }

        [TestMethod]
        public void NegativePaymentCurrentlyAddsMoneyAndEmitsOneNotification()
        {
            var game = Game();
            game.Money = 100;
            var notifications = new List<(string? Name, long Money)>();
            game.PropertyChanged += (_, args) => notifications.Add((args.PropertyName, game.Money));
            // 負額は拒否せず減算の逆として扱う現状。入出金集計へは追加しない。
            game.SpendMoney(-25);
            Assert.AreEqual((125L, 0, 0), (game.Money, game.income, game.outlay));
            CollectionAssert.AreEqual(new[] { ("Money", 125L) }, notifications);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ExpiredRuntimeDeadlineCapturesSameFailureWithOrWithoutGoal(bool hasGoal)
        {
            var game = Game();
            if (hasGoal) game.SelectedMode.goalMoney = long.MaxValue;
            game.SelectedMode.MYear = 2000;
            game.MYear = 1880;
            game.Year = 1881;
            var exception = Assert.ThrowsException<GameOverException>(() => game.NextWeek());
            Assert.AreEqual(new GameOverFailure(GameOverReason.DeadlineExceeded, 1881, 1880), exception.Failure);
            Assert.AreEqual((1881, 1880, 2000), (game.Year, game.MYear, game.SelectedMode.MYear));
            Assert.AreEqual(hasGoal ? long.MaxValue : (long?)null, game.SelectedMode.goalMoney);
        }

        [TestMethod]
        public void MoneyFailureKeepsExtremeRequestedAndAvailableValuesAfterModelChange()
        {
            var game = Game();
            game.Money = long.MinValue;
            var failure = Assert.ThrowsException<MoneyShortException>(() => game.SpendMoney(long.MaxValue)).Failure;
            game.Money = 100;
            // 要求額と資金をそのまま保存し、差額の計算によるオーバーフローを導入しない。
            Assert.AreEqual(new MoneyShortageFailure(long.MaxValue, long.MinValue), failure);
        }

        [TestMethod]
        public void DeadlineFailureKeepsRuntimeYearAndDeadlineAfterModelChange()
        {
            var game = Game();
            game.Year = 1881;
            game.MYear = 1880;
            var failure = Assert.ThrowsException<GameOverException>(() => game.NextWeek()).Failure;
            game.Year = 1900;
            game.MYear = 2300;
            game.SelectedMode.MYear = 2400;
            Assert.AreEqual(new GameOverFailure(GameOverReason.DeadlineExceeded, 1881, 1880), failure);
        }
    }
}
