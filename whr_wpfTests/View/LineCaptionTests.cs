using whr_wpf.Model;
using whr_wpf.ViewModel.Component;
using ModelLine = whr_wpf.Model.Line;

namespace whr_wpf.View.Tests
{
    [TestClass]
    [TestCategory("Display")]
    public class LineCaptionTests
    {
        [TestMethod]
        public void CaptionKeepsHalfWidthSpaceAndFullWidthWaveBetweenNames()
        {
            var line = NamedLine("試験線", "始点", "終点");
            var item = new LineSelectionItem(line);
            Assert.AreEqual("試験線 始点～終点", item.Caption);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void NullOrEmptyNamesKeepInterpolationSeparators(string? name)
        {
            var line = NamedLine(name!, name!, name!);
            var item = new LineSelectionItem(line);
            Assert.AreEqual(" ～", item.Caption);
        }

        [TestMethod]
        public void CaptionReadsCurrentLineAndStationNamesOnEachAccess()
        {
            var line = NamedLine("試験線", "始点", "終点");
            var item = new LineSelectionItem(line);
            string earlier = item.Caption;
            line.Name = "新線";
            line.Start.Name = "新始点";
            line.End.Name = "新終点";
            Assert.AreEqual("新線 新始点～新終点", item.Caption);
            Assert.AreEqual("試験線 始点～終点", earlier);
            line.Start.Name = "";
            Assert.AreEqual("新線 ～新終点", item.Caption);
        }

        private static ModelLine NamedLine(string name, string start, string end) => new()
        {
            Name = name, Start = new Station { Name = start }, End = new Station { Name = end }
        };
    }
}
