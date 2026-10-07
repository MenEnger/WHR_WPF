using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class LineConstructionFailureFormatterTests
    {
        [TestMethod]
        public void InvalidSettingsKeepExistingText()
            => Assert.AreEqual("与えられた引数では路線を建造できません",
                LineConstructionFailureFormatter.Format(new("試験線", 20, RailTypeEnum.Iron, false, RailGaugeEnum.Narrow, 2)));
    }
}
