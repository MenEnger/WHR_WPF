using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("Display")]
    public class MoneyDisplayFormatterTests
    {
        [DataTestMethod]
        [DataRow(0L, "0円")]
        [DataRow(1L, "10万円")]
        [DataRow(-1L, "10万円")]
        [DataRow(999L, "9990万円")]
        [DataRow(1000L, "1億円")]
        [DataRow(9999999L, "9999億9990万円")]
        [DataRow(10000000L, "1兆円")]
        [DataRow(123456789L, "12兆3456億7890万円")]
        [DataRow(100000000000L, "0円")]
        [DataRow(long.MaxValue, "10万円")]
        [DataRow(long.MinValue, "0円")]
        public void CurrentMoneyDisplayKeepsUnitsSignLossTruncationAndOverflow(long amount, string expected)
        {
            // 負額の符号欠落・16桁以上の切捨て・乗算overflowも今回の移動では変えない。
            Assert.AreEqual(expected, MoneyDisplayFormatter.Format(amount));
        }

        [TestMethod]
        public void ProductAtLongMinimumKeepsMathAbsOverflowException()
        {
            Assert.ThrowsException<OverflowException>(() => MoneyDisplayFormatter.Format(288230376151711744L));
        }

    }
}
