using System.IO;
using whr_wpf.Util;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class ScenarioFailureBehaviorTests
    {
        [DataTestMethod]
        [DataRow("town.csv")]
        [DataRow("line.csv")]
        [DataRow("longway.csv")]
        [DataRow("diagram.csv")]
        public void CsvNumericFailureStopsAtTheFirstInvalidFile(string file)
        {
            using var files = new ScenarioFiles();
            var path = Path.Combine(files.DirectoryPath, file);
            var content = File.ReadAllText(path);
            var row = content.Split('\n')[0].Split(',');
            var column = file switch { "town.csv" => 5, "line.csv" => 4, "longway.csv" => 0, _ => 1 };
            row[column] = "invalid";
            File.WriteAllText(path, string.Join(',', row) + "\n");
            var error = Assert.ThrowsException<ScenarioReadException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath));
            Assert.AreEqual(ScenarioReadError.InvalidNumber, error.Error);
            Assert.AreEqual(path, error.FilePath);
            Assert.IsInstanceOfType<FormatException>(error.InnerException);
        }

        [TestMethod]
        public void CsvFailurePrecedesModeFailure()
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("money:5000", "money:invalid");
            File.Delete(Path.Combine(files.DirectoryPath, "town.csv"));
            var error = Assert.ThrowsException<ScenarioReadException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath));
            Assert.AreEqual(ScenarioReadError.FileNotFound, error.Error);
            Assert.AreEqual(ScenarioReadStage.Stations, error.Stage);
            Assert.IsInstanceOfType<FileNotFoundException>(error.InnerException);
        }

        [TestMethod]
        public void ModeFailureOccursAfterMapPreparation()
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("money:5000", "money:invalid");
            var preparations = 0;
            var error = Assert.ThrowsException<ScenarioReadException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath, _ => preparations++));
            Assert.AreEqual(ScenarioReadError.InvalidNumber, error.Error);
            Assert.AreEqual(ScenarioReadStage.Modes, error.Stage);
            Assert.IsInstanceOfType<FormatException>(error.InnerException);
            Assert.AreEqual(1, preparations);
        }

        [TestMethod]
        public void MapCallbackFailureIsPassedThroughAndStopsCsvParsing()
        {
            using var files = new ScenarioFiles();
            File.Delete(Path.Combine(files.DirectoryPath, "town.csv"));
            var failure = new IOException("呼び出し側の画像読込エラー");
            var error = Assert.ThrowsException<IOException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath, _ => throw failure));
            Assert.AreSame(failure, error);
        }
    }
}
