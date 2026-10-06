using System.IO;
using System.Text;
using whr_wpf.Util;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class ScenarioBoundaryTests
    {
        private ScenarioFiles? files;

        [TestCleanup]
        public void Cleanup()
        {
            files?.Dispose();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadingScenarioPreservesDataAndReferenceBindings(bool shiftJis)
        {
            files = new ScenarioFiles(shiftJis);
            var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
            Assert.AreEqual((1880, 100, int.MaxValue, 100, 100),
                (game.BasicYear, game.Rpm, game.SteamYear, game.LineMakeCost, game.TechCost));
            Assert.AreEqual("京都", game.stations[0].Name);
            Assert.AreEqual(1000, game.stations[0].Population);
            var line = game.lines.Single();
            Assert.AreSame(game.stations[0], line.Start);
            Assert.AreSame(game.stations[1], line.End);
            Assert.AreSame(line, game.stations[0].BelongingLines.Single());
            Assert.AreSame(line, game.longwayList.Single().route.Single());
            Assert.AreSame(line, game.diagrams.Single().route.Single());
            Assert.AreSame(game.diagrams.Single(), line.belongingKeitoDiagrams.Single());
            var mode = game.Modes.Single();
            Assert.AreEqual("開始\n案内", mode.Message);
            game.SelectedMode = mode;
            Assert.AreEqual((1880, 1920, 5000L), (game.Year, game.MYear, game.Money));
            Assert.AreEqual(2000, game.stations[0].Population);
        }

        [TestMethod]
        public void MissingScenarioFileRemainsAnIoFailure()
        {
            files = new ScenarioFiles();
            File.Delete(Path.Combine(files.DirectoryPath, "town.csv"));
            var error = Assert.ThrowsException<ScenarioReadException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath));
            Assert.AreEqual(ScenarioReadError.FileNotFound, error.Error);
            Assert.IsInstanceOfType<FileNotFoundException>(error.InnerException);
        }

        [TestMethod]
        public void InvalidNumericValueRemainsAParseFailure()
        {
            files = new ScenarioFiles();
            files.ReplaceProperty("rpm:0", "rpm:invalid");
            var error = Assert.ThrowsException<ScenarioReadException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath));
            Assert.AreEqual(ScenarioReadError.InvalidNumber, error.Error);
            Assert.AreEqual("rpm", error.PropertyName);
            Assert.IsInstanceOfType<FormatException>(error.InnerException);
        }

        [DataTestMethod]
        [DataRow("version:3", "version:0", ScenarioValidationError.InvalidVersion, "version", 0)]
        [DataRow("version:3", "version:-1", ScenarioValidationError.InvalidVersion, "version", -1)]
        [DataRow("basicyear:1880", "basicyear:-1", ScenarioValidationError.InvalidBasicYear, "basicyear", -1)]
        public void InvalidScenarioValueReturnsStructuredFailure(string before, string after,
            ScenarioValidationError reason, string property, int value)
        {
            files = new ScenarioFiles();
            files.ReplaceProperty(before, after);
            // CSV読込へ進まず、ダイアログやApplicationを作らずに拒否される。
            File.Delete(Path.Combine(files.DirectoryPath, "town.csv"));
            var error = Assert.ThrowsException<ScenarioValidationException>(() => ScenerioLoadUtil.LoadFile(files.DirectoryPath));
            Assert.AreEqual(reason, error.Error);
            Assert.AreEqual(property, error.PropertyName);
            Assert.AreEqual(value, error.Value);
            Assert.AreEqual(Path.Combine(files.DirectoryPath, "index.mod"), error.FilePath);
        }

        [TestMethod]
        public void LowestValidVersionAndBasicYearAreAccepted()
        {
            files = new ScenarioFiles();
            files.ReplaceProperty("version:3", "version:1");
            files.ReplaceProperty("basicyear:1880", "basicyear:0");
            var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
            Assert.AreEqual((1, 0), (game.ScenerioVersion, game.BasicYear));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LogicLoadsWithoutAUsableMapImage(bool corrupt)
        {
            files = new ScenarioFiles();
            var mapPath = Path.Combine(files.DirectoryPath, "map.bmp");
            if (corrupt) File.WriteAllText(mapPath, "画像ではない");
            else File.Delete(mapPath);
            var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
            Assert.AreEqual(mapPath, game.MapImagePath);
            Assert.AreEqual(2, game.stations.Count);
            Assert.AreEqual(1, game.lines.Count);
        }

        internal sealed class ScenarioFiles : IDisposable
        {
            public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "whr-scenario-" + Guid.NewGuid());

            public ScenarioFiles(bool shiftJis = false)
            {
                Directory.CreateDirectory(DirectoryPath);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var encoding = shiftJis ? Encoding.GetEncoding(932) : new UTF8Encoding(false);
                File.WriteAllText(Path.Combine(DirectoryPath, "index.mod"), """
                    version:3
                    basicyear:1880
                    season:0
                    steamyear:0
                    kamotu:1
                    km:10
                    rpm:0
                    linemc:0
                    tecc:0
                    infoh:0
                    hojo:1880,1900,10
                    #mode:検証
                    year:1880
                    money:5000
                    message:開始,案内
                    myear:1920
                    people:2,1
                    #end
                    """, encoding);
                File.WriteAllText(Path.Combine(DirectoryPath, "town.csv"), "京都,0,1,10,20,1000,10\n大阪,0,1,30,40,2000,20\n", encoding);
                File.WriteAllText(Path.Combine(DirectoryPath, "line.csv"), "幹線,0,1,1,50,0\n", encoding);
                File.WriteAllText(Path.Combine(DirectoryPath, "longway.csv"), "0,1,1,-1\n", encoding);
                File.WriteAllText(Path.Combine(DirectoryPath, "diagram.csv"), "運転系統,0,1,1,-1\n", encoding);
                // 外部配布物に依存せず、1ピクセルの24ビットBMPを用意する。
                using var writer = new BinaryWriter(File.Create(Path.Combine(DirectoryPath, "map.bmp")));
                writer.Write((ushort)0x4d42);
                writer.Write(58); writer.Write(0); writer.Write(54); writer.Write(40);
                writer.Write(1); writer.Write(1); writer.Write((ushort)1); writer.Write((ushort)24);
                writer.Write(0); writer.Write(4); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                writer.Write(new byte[] { 0, 0, 255, 0 });
            }

            public void ReplaceProperty(string oldValue, string newValue)
            {
                var path = Path.Combine(DirectoryPath, "index.mod");
                File.WriteAllText(path, File.ReadAllText(path).Replace(oldValue, newValue));
            }

            public void Dispose() => Directory.Delete(DirectoryPath, true);
        }
    }
}

