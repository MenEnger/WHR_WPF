using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class GameFailureFormatterTests
    {
        [TestMethod]
        public void MoneyShortageKeepsExistingText()
            => Assert.AreEqual("お金が足りません", GameFailureFormatter.Format(new MoneyShortageFailure(100, 99)));

        [TestMethod]
        public void DeadlineExceededKeepsExistingText()
            => Assert.AreEqual("目標の達成に失敗しました。ゲームオーバーです。",
                GameFailureFormatter.Format(new GameOverFailure(GameOverReason.DeadlineExceeded, 1881, 1880)));

        [TestMethod]
        public void UnknownGameOverReasonIsNotSilentlyDisplayed()
            => Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                GameFailureFormatter.Format(new GameOverFailure((GameOverReason)999, 1881, 1880)));
    }
}
