using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class StockShortageTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UseShortageReportsMissingQuantityBeforeChangingAvailableUnits(bool defaultStock)
        {
            var stock = Stock(defaultStock);
            stock.Release(2);
            var exception = Assert.ThrowsException<StockShortageException>(() => stock.Use(5));
            Assert.AreEqual(new StockShortageFailure(StockShortageOperation.Use, 5, 2), exception.Failure);
            Assert.AreEqual(3, exception.Failure.MissingQuantity);
            Assert.AreEqual(2, stock.HeldUnits);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SaleShortagePrecedesPriceAndPreservesMoneyAndUnits(bool defaultStock)
        {
            var game = Game();
            var stock = Stock(defaultStock);
            stock.Release(2);
            // ユーザー編成では価格を読めない状態も、数量不足なら先に拒否される。
            if (stock is Composition composition) composition.Vehicles = null!;
            var exception = Assert.ThrowsException<StockShortageException>(() => stock.Sale(game, 5));
            Assert.AreEqual(new StockShortageFailure(StockShortageOperation.Sale, 5, 2), exception.Failure);
            Assert.AreEqual(3, exception.Failure.MissingQuantity);
            Assert.AreEqual((1000000L, 2), (game.Money, stock.HeldUnits));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ZeroAndExactAvailableQuantityRemainAcceptedForUseAndSale(bool defaultStock)
        {
            var game = Game();
            var stock = Stock(defaultStock);
            stock.Release(2);
            stock.Use(0);
            Assert.AreEqual(2, stock.HeldUnits);
            stock.Use(2);
            Assert.AreEqual(0, stock.HeldUnits);
            stock.Release(2);
            stock.Sale(game, 0);
            Assert.AreEqual((1000000L, 2), (game.Money, stock.HeldUnits));
            stock.Sale(game, 2);
            Assert.AreEqual((1000020L, 0), (game.Money, stock.HeldUnits));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NegativeQuantityAndNullGameKeepArgumentExceptionsAndState(bool defaultStock)
        {
            var game = Game();
            var stock = Stock(defaultStock);
            stock.Release(2);
            Assert.ThrowsException<ArgumentException>(() => stock.Use(-1));
            Assert.ThrowsException<ArgumentException>(() => stock.Sale(game, -1));
            var exception = Assert.ThrowsException<ArgumentNullException>(() => stock.Sale(null!, 5));
            Assert.AreEqual("gameInfo", exception.ParamName);
            Assert.AreEqual((1000000L, 2), (game.Money, stock.HeldUnits));
        }

        [TestMethod]
        public void UnknownCompositionSaleShortageReadsAvailabilityOnceAndNoOtherProperties()
        {
            var game = Game();
            var observed = new ObservedSaleComposition();
            IComposition stock = observed;
            var exception = Assert.ThrowsException<StockShortageException>(() => stock.Sale(game, 2));
            Assert.AreEqual(new StockShortageFailure(StockShortageOperation.Sale, 2, 1), exception.Failure);
            Assert.AreEqual(1, exception.Failure.MissingQuantity);
            Assert.AreEqual((1, 0, 0, 0), (observed.HeldReads, observed.PriceReads, observed.NameReads, observed.UseCalls));
            Assert.AreEqual(1000000L, game.Money);
        }

        [TestMethod]
        public void SalePriceFailureAfterQuantityCheckIsNotTranslatedToShortage()
        {
            var game = Game();
            var observed = new ObservedSaleComposition();
            IComposition stock = observed;
            var exception = Assert.ThrowsException<FormatException>(() => stock.Sale(game, 1));
            Assert.AreEqual("価格取得失敗", exception.Message);
            Assert.AreEqual((1, 1, 0, 0), (observed.HeldReads, observed.PriceReads, observed.NameReads, observed.UseCalls));
            Assert.AreEqual(1000000L, game.Money);
        }

        [TestMethod]
        public void FailureSnapshotDoesNotFollowLaterAvailableStockChanges()
        {
            var stock = Stock(false);
            stock.Release(2);
            var failure = Assert.ThrowsException<StockShortageException>(() => stock.Use(5)).Failure;
            stock.Release(5);
            Assert.AreEqual(new StockShortageFailure(StockShortageOperation.Use, 5, 2), failure);
            Assert.AreEqual(3, failure.MissingQuantity);
            Assert.AreEqual(7, stock.HeldUnits);
        }

        [TestMethod]
        public void SaleRechecksAvailableStockDuringUseAfterPriceCalculationAndDoesNotCreditMoney()
        {
            var game = Game();
            var observed = new ObservedSaleComposition(consumeDuringPricing: true);
            IComposition stock = observed;
            var exception = Assert.ThrowsException<StockShortageException>(() => stock.Sale(game, 1));
            Assert.AreEqual(new StockShortageFailure(StockShortageOperation.Use, 1, 0), exception.Failure);
            Assert.AreEqual(1, exception.Failure.MissingQuantity);
            Assert.AreEqual((1, 1, 0, 1), (observed.HeldReads, observed.PriceReads, observed.NameReads, observed.UseCalls));
            // 価格取得による在庫変更は残るが、売却処理のUse失敗後は入金されない。
            Assert.AreEqual((1000000L, 0), (game.Money, observed.HeldUnits));
        }

        // default Saleの検査順だけを観測する。その他のIComposition実装は既存クラスを再利用する。
        private sealed class ObservedSaleComposition : Composition, IComposition
        {
            private readonly bool consumeDuringPricing;
            internal int HeldReads, PriceReads, NameReads, UseCalls;
            internal ObservedSaleComposition(bool consumeDuringPricing = false)
            {
                this.consumeDuringPricing = consumeDuringPricing;
                Release(1);
            }
            int IComposition.HeldUnits { get { HeldReads++; return base.HeldUnits; } }
            int IComposition.Price
            {
                get
                {
                    PriceReads++;
                    if (!consumeDuringPricing) throw new FormatException("価格取得失敗");
                    base.Use(1);
                    return 100;
                }
            }
            string IComposition.Name
            {
                get { NameReads++; throw new FormatException("不要な名前取得"); }
                set => throw new InvalidOperationException();
            }
            void IComposition.Use(int quantity) { UseCalls++; base.Use(quantity); }
        }

        private static IComposition Stock(bool defaultStock) => defaultStock
            ? new DefautltComposition { Name = "試験編成", Price = 100 }
            : new Composition { Name = "試験編成", Vehicles = new Dictionary<Car, int> { [new Car { money = 100 }] = 1 } };
    }
}
