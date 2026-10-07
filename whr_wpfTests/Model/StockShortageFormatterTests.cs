using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class StockShortageFormatterTests
    {
        [DataTestMethod]
        [DataRow(StockShortageOperation.Use, "編成数量が3つ不足しています")]
        [DataRow(StockShortageOperation.Sale, "保有編成数が売却数に対して不足しています")]
        public void MessagesKeepExistingText(StockShortageOperation operation, string expected)
            => Assert.AreEqual(expected, StockShortageFormatter.Format(new(operation, 5, 2)));

        [TestMethod]
        public void MissingQuantityKeepsExistingIntArithmetic()
        {
            // 異常な負在庫でも、文言分離で旧int減算の表示を変更しない。
            var failure = new StockShortageFailure(StockShortageOperation.Use, int.MaxValue, -1);
            Assert.AreEqual(int.MinValue, failure.MissingQuantity);
            Assert.AreEqual($"編成数量が{int.MinValue}つ不足しています", StockShortageFormatter.Format(failure));
        }

        [TestMethod]
        public void UnknownOperationIsNotSilentlyDisplayed()
            => Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                StockShortageFormatter.Format(new((StockShortageOperation)999, 5, 2)));
    }
}
