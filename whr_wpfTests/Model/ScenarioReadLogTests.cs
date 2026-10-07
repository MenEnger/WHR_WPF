using System.IO;
using whr_wpf.Util;
using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class ScenarioReadLogTests
    {
        [TestMethod]
        public void DiagnosticFileContainsSourceInnerExceptionAndStackTrace()
        {
            using var files = new ScenarioFiles();
            var logPath = Path.Combine(files.DirectoryPath, "Logs", "scenario-read.log");
            Exception inner;
            try { int.Parse("数値ではない"); throw new AssertFailedException(); }
            catch (FormatException ex) { inner = ex; }
            var error = new ScenarioReadException(ScenarioReadError.InvalidNumber, ScenarioReadStage.Modes,
                "index.mod", inner, 23, "money", 2);
            Assert.AreEqual(Path.GetFullPath(logPath), ScenarioReadDiagnostics.TryWrite(error, logPath));
            var content = File.ReadAllText(logPath);
            foreach (var value in new[] { "Reason: InvalidNumber", "Stage: Modes", "File: index.mod",
                "Property: money", "Line: 23", "Mode: 2", inner.ToString() })
                StringAssert.Contains(content, value);
            Assert.IsTrue(DateTimeOffset.TryParse(content.Split('\n')[0].Trim('[', ']', '\r'), out var timestamp));
            Assert.AreEqual(TimeSpan.Zero, timestamp.Offset);
        }

        [TestMethod]
        public void UnexpectedFailuresAppendWithoutReplacingEarlierRecords()
        {
            using var files = new ScenarioFiles();
            var logPath = Path.Combine(files.DirectoryPath, "scenario-read.log");
            Assert.IsNotNull(ScenarioReadDiagnostics.TryWrite(new IOException("最初の画像失敗"), logPath));
            var first = File.ReadAllText(logPath);
            Assert.IsNotNull(ScenarioReadDiagnostics.TryWrite(new FormatException("次の失敗"), logPath));
            var combined = File.ReadAllText(logPath);
            Assert.IsTrue(combined.StartsWith(first));
            StringAssert.Contains(combined, "最初の画像失敗");
            StringAssert.Contains(combined, "次の失敗");
        }

        [TestMethod]
        public void DirectoryCreationFailureDoesNotReplaceTheReadFailure()
        {
            using var files = new ScenarioFiles();
            var blocker = Path.Combine(files.DirectoryPath, "blocked");
            File.WriteAllText(blocker, "既存ファイル");
            var logPath = Path.Combine(blocker, "scenario-read.log");
            Assert.IsNull(ScenarioReadDiagnostics.TryWrite(new IOException("元の読込失敗"), logPath));
            Assert.AreEqual("既存ファイル", File.ReadAllText(blocker));
        }

        [TestMethod]
        public void AppendFailureLeavesExistingLogAndReturnsNoSavedPath()
        {
            using var files = new ScenarioFiles();
            var logPath = Path.Combine(files.DirectoryPath, "scenario-read.log");
            File.WriteAllText(logPath, "既存ログ");
            using (var locked = File.Open(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.IsNull(ScenarioReadDiagnostics.TryWrite(new IOException("元の読込失敗"), logPath));
            Assert.AreEqual("既存ログ", File.ReadAllText(logPath));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LogStatusPreservesTheOriginalGuidance(bool saved)
        {
            const string original = "必要なファイルが見つかりません。";
            var logPath = saved ? Path.Combine(Path.GetTempPath(), "scenario-read.log") : null;
            var message = ScenarioPresentation.WithDiagnosticLog(original, logPath);
            Assert.IsTrue(message.StartsWith(original));
            if (saved) StringAssert.Contains(message, logPath!);
            else StringAssert.Contains(message, "診断ログを保存できませんでした。");
        }
    }
}
