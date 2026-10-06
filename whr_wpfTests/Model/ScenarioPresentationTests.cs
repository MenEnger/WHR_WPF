using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Media;
using whr_wpf.Util;
using whr_wpf.View;
using static whr_wpf.Model.Tests.ScenarioBoundaryTests;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class ScenarioPresentationTests
    {
        [TestMethod]
        public void MapRenderingReusesUiImageAndReleasesTheSourceFile()
        {
            OnStaThread(() =>
            {
                using var files = new ScenarioFiles();
                var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
                game.SelectedMode = game.Modes.Single();
                var page = new GamePage(game);
                page.DrawMap();
                var canvas = (Canvas)page.FindName("MapCanvas");
                var image = ((ImageBrush)canvas.Background).ImageSource;
                Assert.IsTrue(image.IsFrozen);
                Assert.AreEqual(1d, image.Width);
                // 画像を表示したまま元ファイルを消せる。再描画で再読込しない。
                File.Delete(game.MapImagePath);
                page.DrawMap();
                Assert.AreSame(image, ((ImageBrush)canvas.Background).ImageSource);
            });
        }

        [TestMethod]
        public void LoadedImageSurvivesSourceDeletionBeforeGameStarts()
        {
            OnStaThread(() =>
            {
                using var files = new ScenarioFiles();
                var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
                var presentation = new ScenarioPresentation(game);
                // メニュー相当の読込後、選択画面からゲームへ進む前に消しても表示できる。
                File.Delete(game.MapImagePath);
                _ = new DifficultyLevelSelectPage(presentation);
                _ = new ModeSelectPage(presentation);
                game.SelectedMode = game.Modes.Single();
                var page = new GamePage(presentation);
                page.DrawMap();
                var canvas = (Canvas)page.FindName("MapCanvas");
                Assert.AreSame(presentation.MapImage, ((ImageBrush)canvas.Background).ImageSource);
                page.DrawMap();
                Assert.AreSame(presentation.MapImage, ((ImageBrush)canvas.Background).ImageSource);
                Assert.AreEqual((1880, 1920, 5000L), (game.Year, game.MYear, game.Money));
            });
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ImageSnapshotSurvivesSourceChangeBeforeCsvParsing(bool replace)
        {
            OnStaThread(() =>
            {
                using var files = new ScenarioFiles();
                System.Windows.Media.Imaging.BitmapImage? snapshot = null;
                var readCount = 0;
                var presentation = ScenarioPresentation.LoadScenario(files.DirectoryPath, path =>
                {
                    readCount++;
                    snapshot = ScenarioPresentation.LoadMap(path);
                    // 画像確保とCSV解析の間の変更を、並行処理のタイミングに依存せず再現する。
                    if (replace) File.WriteAllText(path, "置換された画像");
                    else File.Delete(path);
                    return snapshot;
                });
                Assert.AreEqual(1, readCount);
                Assert.AreSame(snapshot, presentation.MapImage);
                Assert.AreEqual(2, presentation.GameInfo.stations.Count);
                presentation.GameInfo.SelectedMode = presentation.GameInfo.Modes.Single();
                var page = new GamePage(presentation);
                page.DrawMap();
                Assert.AreSame(snapshot, ((ImageBrush)((Canvas)page.FindName("MapCanvas")).Background).ImageSource);
            });
        }

        [TestMethod]
        public void MapFailurePrecedesCsvFailure()
        {
            using var files = new ScenarioFiles();
            var mapPath = Path.Combine(files.DirectoryPath, "map.bmp");
            File.Delete(mapPath);
            File.Delete(Path.Combine(files.DirectoryPath, "town.csv"));
            var error = Assert.ThrowsException<FileNotFoundException>(() => ScenarioPresentation.LoadScenario(files.DirectoryPath));
            Assert.AreEqual(mapPath, error.FileName);
        }

        [TestMethod]
        public void InvalidSettingsPrecedeMapPreparation()
        {
            using var files = new ScenarioFiles();
            files.ReplaceProperty("version:3", "version:0");
            var readCount = 0;
            Assert.ThrowsException<ScenarioValidationException>(() => ScenarioPresentation.LoadScenario(files.DirectoryPath, path =>
            {
                readCount++;
                throw new AssertFailedException("不正な設定の後に画像を読んではいけない");
            }));
            Assert.AreEqual(0, readCount);
        }

        [TestMethod]
        public void CsvFailureDoesNotReturnPartialPresentation()
        {
            using var files = new ScenarioFiles();
            var townPath = Path.Combine(files.DirectoryPath, "town.csv");
            File.Delete(townPath);
            var readCount = 0;
            var error = Assert.ThrowsException<ScenarioReadException>(() => ScenarioPresentation.LoadScenario(files.DirectoryPath, path =>
            {
                readCount++;
                return ScenarioPresentation.LoadMap(path);
            }));
            Assert.AreEqual(1, readCount);
            Assert.AreEqual(townPath, error.FilePath);
            Assert.AreEqual(ScenarioReadError.FileNotFound, error.Error);
            Assert.IsInstanceOfType<FileNotFoundException>(error.InnerException);
            // CSV失敗時にも、先に確保した画像の元ファイルは開いたままにしない。
            File.Delete(Path.Combine(files.DirectoryPath, "map.bmp"));
        }

        [TestMethod]
        public void MissingMapIsRejectedByPresentation()
        {
            using var files = new ScenarioFiles();
            var path = Path.Combine(files.DirectoryPath, "map.bmp");
            File.Delete(path);
            Assert.ThrowsException<FileNotFoundException>(() => ScenarioPresentation.LoadMap(path));
        }

        [TestMethod]
        public void CorruptMapIsRejectedByPresentation()
        {
            OnStaThread(() =>
            {
                using var files = new ScenarioFiles();
                var path = Path.Combine(files.DirectoryPath, "map.bmp");
                File.WriteAllText(path, "画像ではない");
                Assert.ThrowsException<NotSupportedException>(() => ScenarioPresentation.LoadMap(path));
            });
        }

        [DataTestMethod]
        [DataRow(ScenarioValidationError.InvalidVersion, "シナリオバージョンの数値が異常です")]
        [DataRow(ScenarioValidationError.InvalidBasicYear, "基礎とする年の値が異常です")]
        public void ValidationMessagesAreFormattedByPresentation(ScenarioValidationError reason, string message)
        {
            var error = new ScenarioValidationException(reason, "index.mod", "property", -1);
            Assert.AreEqual(message, ScenarioPresentation.FormatValidationError(error));
        }

        private static void OnStaThread(Action action)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
