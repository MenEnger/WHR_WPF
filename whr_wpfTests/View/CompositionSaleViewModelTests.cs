using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("AdoptedOriginalState")]
    public class CompositionSaleViewModelTests
    {
        [TestMethod]
        public void SaleScreenQuotesTheSameRoundedTotalAsTheModel()
        {
            var game = Game();
            IComposition stock = SaleStock(game, true);
            // 内部VMを生成して表示文字列だけ確認し、確認ダイアログは開かない。
            var type = typeof(whr_wpf.ViewModel.ViewModelBase).Assembly.GetType("whr_wpf.ViewModel.Vehicle.CompositionManageViewModel")!;
            var vm = Activator.CreateInstance(type, new object?[] { game, null })!;
            type.GetProperty("Composition")!.SetValue(vm, stock);
            type.GetProperty("Quantity")!.SetValue(vm, 2);
            var quote = (string)type.GetProperty("PriceInfo")!.GetValue(vm)!;
            StringAssert.Contains(quote, "合計売却価格　30万円");
            long money = game.Money;
            stock.Sale(game, 2);
            Assert.AreEqual(3L, game.Money - money);
        }

    }
}
