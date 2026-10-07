using System.IO;
using whr_wpf.Util;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class DiagnosticBoundaryTests
    {
        [TestMethod]
        public void UnassignedLineReturnsZeroWithoutWritingToHostConsole()
        {
            var game = Game();
            var line = Line(game);
            var before = (game.Money, line.totalBalance, line.bestSpeed, line.IsExist);
            AssertNoConsoleOutput(() =>
            {
                Assert.AreEqual(0, line.CalcBestSpeed(null!));
                Assert.AreEqual(before, (game.Money, line.totalBalance, line.bestSpeed, line.IsExist));
            });
        }

        [TestMethod]
        public void ScenarioLoadingAndModeApplicationDoNotWriteToHostConsole()
        {
            using var files = new ScenarioFiles();
            AssertNoConsoleOutput(() =>
            {
                var game = ScenerioLoadUtil.LoadFile(files.DirectoryPath);
                game.SelectedMode = game.Modes.Single();
                Assert.AreEqual((1880, 1920, 5000L), (game.Year, game.MYear, game.Money));
                Assert.AreEqual(2000, game.stations[0].Population);
                Assert.AreSame(game.lines.Single(), game.stations[0].BelongingLines.Single());
            });
        }

        private static void AssertNoConsoleOutput(Action action)
        {
            // ゲームを呼び出すホストの標準出力へ診断が混入しないことを守る。
            var originalOutput = Console.Out;
            var originalError = Console.Error;
            using var output = new StringWriter();
            using var error = new StringWriter();
            try
            {
                Console.SetOut(output);
                Console.SetError(error);
                action();
                Assert.AreEqual(string.Empty, output.ToString());
                Assert.AreEqual(string.Empty, error.ToString());
            }
            finally
            {
                Console.SetOut(originalOutput);
                Console.SetError(originalError);
            }
        }
    }
}
