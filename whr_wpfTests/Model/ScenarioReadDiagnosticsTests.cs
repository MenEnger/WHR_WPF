using System.IO;
using Microsoft.VisualBasic.FileIO;
using whr_wpf.Util;
using whr_wpf.View;
using static whr_wpf.Model.Tests.ScenarioBoundaryTests;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class ScenarioReadDiagnosticsTests
    {
        [DataTestMethod]
        [DataRow("rpm:0", "", "rpm", ScenarioReadError.MissingValue)]
        [DataRow("rpm:0", "rpm:", "rpm", ScenarioReadError.InvalidNumber)]
        [DataRow("rpm:0", "rpm:no", "rpm", ScenarioReadError.InvalidNumber)]
        [DataRow("rpm:0", "rpm:2147483648", "rpm", ScenarioReadError.NumberOutOfRange)]
        [DataRow("hojo:1880,1900,10", "", "hojo", ScenarioReadError.MissingValue)]
        [DataRow("hojo:1880,1900,10", "hojo:1880", "hojo", ScenarioReadError.MissingField)]
        [DataRow("hojo:1880,1900,10", "hojo:1880,no,10", "hojo", ScenarioReadError.InvalidNumber)]
        [DataRow("hojo:1880,1900,10", "hojo:1880,1900,2147483648", "hojo", ScenarioReadError.NumberOutOfRange)]
        public void InvalidSettingsIdentifyTheConsumedProperty(string before, string after, string property, ScenarioReadError reason)
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty(before, after);
            // 後続の資源にも失敗を置き、消費順を変えていないことを検査する。
            File.Delete(Path.Combine(files.DirectoryPath, "town.csv"));
            var called = false;
            var error = Assert.ThrowsException<ScenarioReadException>(() =>
                ScenerioLoadUtil.LoadFile(files.DirectoryPath, _ => called = true));
            Assert.IsFalse(called);
            AssertSource(error, files, "index.mod", ScenarioReadStage.Settings, reason);
            Assert.AreEqual(property, error.PropertyName);
            Assert.IsNull(error.ModeNumber);
            if (reason == ScenarioReadError.MissingValue) Assert.IsNull(error.InnerException);
            if (reason == ScenarioReadError.InvalidNumber) Assert.IsInstanceOfType<FormatException>(error.InnerException);
            if (reason == ScenarioReadError.NumberOutOfRange) Assert.IsInstanceOfType<OverflowException>(error.InnerException);
        }

        [DataTestMethod]
        [DataRow("index.mod", ScenarioReadStage.Settings)]
        [DataRow("town.csv", ScenarioReadStage.Stations)]
        [DataRow("line.csv", ScenarioReadStage.Lines)]
        [DataRow("longway.csv", ScenarioReadStage.Longways)]
        [DataRow("diagram.csv", ScenarioReadStage.Diagrams)]
        public void MissingFilesKeepTheirSourceAndInnerException(string file, ScenarioReadStage stage)
        {
            using var files = new ScenarioFiles();
            File.Delete(Path.Combine(files.DirectoryPath, file));
            var error = ReadFailure(files);
            AssertSource(error, files, file, stage, ScenarioReadError.FileNotFound);
            Assert.IsInstanceOfType<FileNotFoundException>(error.InnerException);
        }

        [TestMethod]
        public void MissingDirectoryIsAFileMissingDiagnostic()
        {
            using var files = new ScenarioFiles();
            var error = Assert.ThrowsException<ScenarioReadException>(() =>
                ScenerioLoadUtil.LoadFile(Path.Combine(files.DirectoryPath, "存在しないフォルダー")));
            Assert.AreEqual(ScenarioReadError.FileNotFound, error.Error);
            Assert.AreEqual(ScenarioReadStage.Settings, error.Stage);
            Assert.IsInstanceOfType<DirectoryNotFoundException>(error.InnerException);
        }

        [DataTestMethod]
        [DataRow("index.mod", ScenarioReadStage.Settings)]
        [DataRow("town.csv", ScenarioReadStage.Stations)]
        public void ExclusiveFileLockProducesAnIoDiagnostic(string file, ScenarioReadStage stage)
        {
            using var files = new ScenarioFiles();
            using var locked = File.Open(Path.Combine(files.DirectoryPath, file), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            var error = ReadFailure(files);
            AssertSource(error, files, file, stage, ScenarioReadError.IoFailure);
            Assert.IsInstanceOfType<IOException>(error.InnerException);
        }

        [TestMethod]
        public void ADirectoryAtTheRequiredFilePathProducesAccessDenied()
        {
            using var files = new ScenarioFiles();
            var path = Path.Combine(files.DirectoryPath, "index.mod");
            File.Delete(path);
            Directory.CreateDirectory(path);
            var error = ReadFailure(files);
            AssertSource(error, files, "index.mod", ScenarioReadStage.Settings, ScenarioReadError.AccessDenied);
            Assert.IsInstanceOfType<UnauthorizedAccessException>(error.InnerException);
        }

        [DataTestMethod]
        [DataRow("warmode:1880", ScenarioReadError.MissingField)]
        [DataRow("warmode:1880,no,1", ScenarioReadError.InvalidNumber)]
        public void WarModeErrorsBelongToTheSettingsStage(string property, ScenarioReadError reason)
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("hojo:1880,1900,10", "hojo:1880,1900,10\n" + property);
            var error = ReadFailure(files);
            AssertSource(error, files, "index.mod", ScenarioReadStage.Settings, reason);
            Assert.AreEqual("warmode", error.PropertyName);
            Assert.IsNotNull(error.InnerException);
        }

        [TestMethod]
        public void PropertyPrefixMatchingRemainsCompatible()
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("rpm:0", "rpmExtra:125");
            var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
            Assert.AreEqual(125, game.Rpm);
        }

        [TestMethod]
        public void LongwayFreightDisabledTerminatorPreservesItsRoute()
        {
            using var files = new ScenarioFiles();
            File.WriteAllText(Path.Combine(files.DirectoryPath, "longway.csv"), "0,1,1,-2\n");
            var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
            var longway = game.longwayList.Single();
            Assert.IsFalse(longway.isKamotuOperated);
            Assert.AreSame(game.lines.Single(), longway.route.Single());
            Assert.AreSame(game.stations[0], longway.start);
            Assert.AreSame(game.stations[1], longway.end);
        }

        [TestMethod]
        public void NumericConsumptionStillPrecedesTheExistingValueValidation()
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("version:3", "version:0");
            files.ReplaceProperty("rpm:0", "rpm:no");
            var error = ReadFailure(files);
            Assert.AreEqual(ScenarioReadError.InvalidNumber, error.Error);
            Assert.AreEqual("rpm", error.PropertyName);
        }

        [DataTestMethod]
        [DataRow("town.csv", "京都,0,1,10,20,no,10\n", ScenarioReadStage.Stations, ScenarioReadError.InvalidNumber)]
        [DataRow("town.csv", "京都,0,1,10,20,2147483648,10\n", ScenarioReadStage.Stations, ScenarioReadError.NumberOutOfRange)]
        [DataRow("town.csv", "京都,0,1\n", ScenarioReadStage.Stations, ScenarioReadError.MissingField)]
        [DataRow("line.csv", "幹線,0\n", ScenarioReadStage.Lines, ScenarioReadError.MissingField)]
        [DataRow("line.csv", "幹線,2,1,1,50,0\n", ScenarioReadStage.Lines, ScenarioReadError.InvalidReference)]
        [DataRow("line.csv", "幹線,-1,1,1,50,0\n", ScenarioReadStage.Lines, ScenarioReadError.InvalidReference)]
        [DataRow("longway.csv", "0\n", ScenarioReadStage.Longways, ScenarioReadError.MissingField)]
        [DataRow("longway.csv", "0,1,2,-1\n", ScenarioReadStage.Longways, ScenarioReadError.InvalidReference)]
        [DataRow("longway.csv", "0,1,1\n", ScenarioReadStage.Longways, ScenarioReadError.InvalidFormat)]
        [DataRow("diagram.csv", "系統,0\n", ScenarioReadStage.Diagrams, ScenarioReadError.MissingField)]
        [DataRow("diagram.csv", "系統,0,1,0,-1\n", ScenarioReadStage.Diagrams, ScenarioReadError.InvalidReference)]
        [DataRow("diagram.csv", "系統,0,1,1\n", ScenarioReadStage.Diagrams, ScenarioReadError.InvalidFormat)]
        public void CsvFailureDistinguishesNumbersColumnsReferencesAndTerminators(string file, string data,
            ScenarioReadStage stage, ScenarioReadError reason)
        {
            using var files = new ScenarioFiles();
            File.WriteAllText(Path.Combine(files.DirectoryPath, file), data);
            var error = ReadFailure(files);
            AssertSource(error, files, file, stage, reason);
            Assert.AreEqual(1L, error.LineNumber);
            Assert.IsNotNull(error.InnerException);
        }

        [DataTestMethod]
        [DataRow("\n\n京都,0,1,10,20,no,10\n", 3L)]
        [DataRow("\n   \n京都,0,1,10,20,no,10\n", 3L)]
        [DataRow("\n\t \t\n京都,0,1,10,20,no,10\n", 3L)]
        [DataRow("\n\u3000\n京都,0,1,10,20,no,10\n", 3L)]
        [DataRow("\n\"京都\n駅\",0,1,10,20,no,10\n", 2L)]
        [DataRow("\"京都\n駅\",0,1,10,20,1000,10\n\n大阪,0,1,30,40,no,20\n", 4L)]
        public void CsvNumericDiagnosticsUsePhysicalRecordStartLine(string data, long expectedLine)
        {
            using var files = new ScenarioFiles();
            File.WriteAllText(Path.Combine(files.DirectoryPath, "town.csv"), data);
            var error = ReadFailure(files);
            Assert.AreEqual(ScenarioReadError.InvalidNumber, error.Error);
            Assert.AreEqual(expectedLine, error.LineNumber);
        }

        [TestMethod]
        public void MalformedQuotedCsvPreservesTheParserErrorAndLine()
        {
            using var files = new ScenarioFiles();
            File.WriteAllText(Path.Combine(files.DirectoryPath, "town.csv"), "\n\"京都,0,1,10,20,1000,10\n");
            var error = ReadFailure(files);
            AssertSource(error, files, "town.csv", ScenarioReadStage.Stations, ScenarioReadError.InvalidFormat);
            Assert.IsInstanceOfType<MalformedLineException>(error.InnerException);
            Assert.AreEqual(2L, error.LineNumber);
        }

        [DataTestMethod]
        [DataRow("money:5000", "money:no", "money", ScenarioReadError.InvalidNumber)]
        [DataRow("money:5000", "money:9223372036854775808", "money", ScenarioReadError.NumberOutOfRange)]
        [DataRow("\nyear:1880", "", "year", ScenarioReadError.MissingValue)]
        [DataRow("message:開始,案内", "", "message", ScenarioReadError.MissingValue)]
        [DataRow("message:開始,案内", "message", "message", ScenarioReadError.MissingField)]
        public void ModeErrorsIdentifyTheirBlockAndProperty(string before, string after, string property, ScenarioReadError reason)
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty(before, after);
            var error = ReadFailure(files);
            AssertSource(error, files, "index.mod", ScenarioReadStage.Modes, reason);
            Assert.AreEqual(1, error.ModeNumber);
            Assert.AreEqual(property, error.PropertyName);
        }

        [TestMethod]
        public void SecondModeFailureHasItsOwnBlockNumber()
        {
            using var files = new ScenarioFiles();
            File.AppendAllText(Path.Combine(files.DirectoryPath, "index.mod"),
                "\n#mode:二つ目\nyear:1880\nmoney:no\nmessage:案内\nmyear:1920\n#end\n");
            var error = ReadFailure(files);
            Assert.AreEqual(2, error.ModeNumber);
            Assert.AreEqual("money", error.PropertyName);
            Assert.AreEqual(ScenarioReadError.InvalidNumber, error.Error);
        }

        [DataTestMethod]
        [DataRow("car:試験,80,5,1,1,10,1\nltn:1,1\nulc:1", "ulc", ScenarioReadError.MissingField)]
        [DataRow("car:試験,80,5,1,1,10,1\nltn:1\nulc:2", "ulc", ScenarioReadError.InvalidReference)]
        [DataRow("car:試験,80,5,1,1,10,1\nudc:1,1\nudcr:2", "udcr", ScenarioReadError.MissingField)]
        [DataRow("car:試験,80,5,1,1,10,1\nudc:0", "udc", ScenarioReadError.InvalidReference)]
        [DataRow("mtec:1,40\nmtec:1,80", "mtec", ScenarioReadError.InvalidSetting)]
        [DataRow("car:試験,80,4,1,1,10,0", "car", ScenarioReadError.InvalidSetting)]
        [DataRow("car:試験,80,1,1,1,10,0", "car", ScenarioReadError.InvalidSetting)]
        [DataRow("car:試験,80,5,1,1,10,-1", "car", ScenarioReadError.InvalidSetting)]
        [DataRow("car:試験,80", "car", ScenarioReadError.MissingField)]
        public void ModeArrayAndCompositionErrorsRemainAtTheirConsumptionPoint(string properties, string property, ScenarioReadError reason)
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("#end", properties + "\n#end");
            var error = ReadFailure(files);
            AssertSource(error, files, "index.mod", ScenarioReadStage.Modes, reason);
            Assert.AreEqual(property, error.PropertyName);
            Assert.AreEqual(1, error.ModeNumber);
            Assert.IsNotNull(error.InnerException);
        }

        [TestMethod]
        public void PreviouslyPermittedOmissionsAndShortGuardedArraysRemainAccepted()
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("message:開始,案内", "message:");
            files.ReplaceProperty("people:2,1", "people:2");
            files.ReplaceProperty("#end", "car:試験,80,5,1,1,10,1\nltn:1,1\nulc:1,1\nulcr:3\n#end");
            var mode = ScenerioLoadUtil.LoadFile(files.DirectoryPath).Modes.Single();
            Assert.AreEqual("", mode.Message);
            Assert.AreEqual(40, mode.genkaiJoki);
            Assert.IsNull(mode.genkaiDenki);
            Assert.AreEqual(2, mode.LineSettings.Count);
            Assert.AreEqual(3, mode.LineSettings[0].runningPerDay);
            Assert.AreEqual(0, mode.LineSettings[1].runningPerDay);
        }

        [DataTestMethod]
        [DataRow("town.csv", "line.csv", ScenarioReadStage.Stations)]
        [DataRow("line.csv", "longway.csv", ScenarioReadStage.Lines)]
        [DataRow("longway.csv", "diagram.csv", ScenarioReadStage.Longways)]
        public void EarlierCsvFailurePrecedesLaterCsvFailure(string first, string second, ScenarioReadStage stage)
        {
            using var files = new ScenarioFiles();
            File.Delete(Path.Combine(files.DirectoryPath, first));
            File.Delete(Path.Combine(files.DirectoryPath, second));
            var error = ReadFailure(files);
            AssertSource(error, files, first, stage, ScenarioReadError.FileNotFound);
        }

        [TestMethod]
        public void MapCallbackFormatExceptionIsNotConverted()
        {
            using var files = new ScenarioFiles();
            File.Delete(Path.Combine(files.DirectoryPath, "town.csv"));
            var failure = new FormatException("地図側の独自解析エラー");
            var error = Assert.ThrowsException<FormatException>(() =>
                ScenerioLoadUtil.LoadFile(files.DirectoryPath, _ => throw failure));
            Assert.AreSame(failure, error);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CsvChangesAfterMapPreparationAreDiagnosedAtTheCsvStage(bool replace)
        {
            using var files = new ScenarioFiles();
            var error = Assert.ThrowsException<ScenarioReadException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath, _ =>
            {
                var path = Path.Combine(files.DirectoryPath, "town.csv");
                if (replace) File.WriteAllText(path, "京都,0,1,10,20,no,10\n");
                else File.Delete(path);
            }));
            AssertSource(error, files, "town.csv", ScenarioReadStage.Stations,
                replace ? ScenarioReadError.InvalidNumber : ScenarioReadError.FileNotFound);
        }

        [TestMethod]
        public void ModesUseTheIndexSnapshotWhenTheFileChangesAfterMapPreparation()
        {
            using var files = new ScenarioFiles();
            var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath, _ =>
                File.WriteAllText(Path.Combine(files.DirectoryPath, "index.mod"), "完全に置換した設定"));
            Assert.AreEqual(5000L, game.Modes.Single().Money);
            Assert.AreEqual("開始\n案内", game.Modes.Single().Message);
        }

        [TestMethod]
        public void PublicParsingHelpersKeepTheirOriginalFailureContracts()
        {
            Assert.ThrowsException<FormatException>(() => ScenerioLoadUtil.ParseIntOrNull("no"));
            Assert.ThrowsException<IndexOutOfRangeException>(() => ScenerioLoadUtil.ExtractModProperty(["rpm"], "rpm"));
            Assert.ThrowsException<IndexOutOfRangeException>(() => ScenerioLoadUtil.CreateWarModeList(["1880"]));
            Assert.ThrowsException<FormatException>(() => ScenerioLoadUtil.CreateModeList(
                ["#mode:検証", "year:no", "money:1", "message:案内", "myear:1920", "#end"]));
        }

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

        private static ScenarioReadException ReadFailure(ScenarioFiles files) =>
            Assert.ThrowsException<ScenarioReadException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath));

        private static void AssertSource(ScenarioReadException error, ScenarioFiles files, string file,
            ScenarioReadStage stage, ScenarioReadError reason)
        {
            Assert.AreEqual(reason, error.Error);
            Assert.AreEqual(stage, error.Stage);
            Assert.AreEqual(Path.Combine(files.DirectoryPath, file), error.FilePath);
        }
    }
}
