using whr_wpf.Util;
using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class ScenarioReadFailureFormatterTests
    {
        [DataTestMethod]
        [DataRow(ScenarioReadError.FileNotFound)]
        [DataRow(ScenarioReadError.AccessDenied)]
        [DataRow(ScenarioReadError.IoFailure)]
        [DataRow(ScenarioReadError.MissingValue)]
        [DataRow(ScenarioReadError.InvalidNumber)]
        [DataRow(ScenarioReadError.NumberOutOfRange)]
        [DataRow(ScenarioReadError.InvalidFormat)]
        [DataRow(ScenarioReadError.MissingField)]
        [DataRow(ScenarioReadError.InvalidReference)]
        [DataRow(ScenarioReadError.InvalidSetting)]
        public void UiMessagesUseTheReasonAndSourceWithoutExposingInternalMessages(ScenarioReadError reason)
        {
            const string secret = "INTERNAL_SECRET_STACK_AND_RAW_VALUE";
            var error = new ScenarioReadException(reason, ScenarioReadStage.Modes, "index.mod",
                new Exception(secret), 23, "money", 2);
            var message = ScenarioPresentation.FormatReadError(error);
            StringAssert.Contains(message, "index.mod");
            StringAssert.Contains(message, "money");
            StringAssert.Contains(message, "23");
            StringAssert.Contains(message, "2");
            Assert.IsFalse(message.Contains(secret));
            Assert.IsTrue(message.Any(c => c >= '\u3040' && c <= '\u9fff'));
            // 出典が取れない失敗や、明示的な欠損検査にも対応する。
            var unknownSource = ScenarioPresentation.FormatReadError(
                new ScenarioReadException(reason, ScenarioReadStage.Settings, "index.mod"));
            StringAssert.Contains(unknownSource, "index.mod");
            Assert.IsFalse(string.IsNullOrWhiteSpace(unknownSource));
        }

        [TestMethod]
        public void UiReasonsHaveDifferentGuidanceAndUnexpectedFailureHasGenericGuidance()
        {
            var messages = Enum.GetValues<ScenarioReadError>().Select(reason => ScenarioPresentation.FormatReadError(
                new ScenarioReadException(reason, ScenarioReadStage.Settings, "index.mod"))).ToList();
            Assert.AreEqual(messages.Count, messages.Distinct().Count());
            var unexpected = ScenarioPresentation.FormatUnexpectedReadError();
            Assert.IsFalse(string.IsNullOrWhiteSpace(unexpected));
            Assert.IsTrue(unexpected.Any(c => c >= '\u3040' && c <= '\u9fff'));
        }

    }
}
