using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class CompositionRegistrationTests
    {
        [TestMethod]
        public void CreationRegistersReturnedCompositionOnceWithoutBuyingStock()
        {
            var game = Game();
            var stock = Stock(game, 3);
            var car = new Car { bestSpeed = 60, power = PowerEnum.Steam, gauge = CarGaugeEnum.Narrow, type = RailTypeEnum.Iron, seat = SeatEnum.Semi, money = 100 };
            var omitted = new Car { bestSpeed = 10, power = PowerEnum.Diesel, type = RailTypeEnum.LinearMotor };
            game.vehicles.Add(car);
            long money = game.Money;

            var result = game.CreateComposition("登録編成", new Dictionary<Car, int> { [car] = 2, [omitted] = 0 });

            Assert.AreEqual(2, game.compositions.Count);
            Assert.AreSame(stock, game.compositions[0]);
            Assert.AreSame(result, game.compositions[1]);
            Assert.AreEqual("登録編成", result.Name);
            Assert.AreEqual(1, result.Vehicles.Count);
            Assert.AreEqual(2, result.Vehicles[car]);
            Assert.AreEqual((money, 3, 0), (game.Money, stock.HeldUnits, result.HeldUnits));
        }

        [TestMethod]
        public void CreationRejectionKeepsRegistrationMoneyAndExistingStock()
        {
            var game = Game();
            var stock = Stock(game, 3);
            long money = game.Money;

            var error = Assert.ThrowsException<CompositionCreationRejectedException>(() =>
                game.CreateComposition("登録編成", Array.Empty<KeyValuePair<Car, int>>()));

            Assert.AreEqual(CompositionCreationReason.NoVehicles, error.Check.Reason);
            Assert.AreEqual(1, game.compositions.Count);
            Assert.AreSame(stock, game.compositions[0]);
            Assert.AreEqual((money, 3), (game.Money, stock.HeldUnits));
        }

        [TestMethod]
        public void EnumerationFailurePropagatesTheSameExceptionBeforeRegistration()
        {
            var game = Game();
            var stock = Stock(game, 3);
            long money = game.Money;
            var failure = new IOException("列挙途中の失敗");
            IEnumerable<KeyValuePair<Car, int>> Input()
            {
                yield return new(new Car { bestSpeed = 60 }, 1);
                throw failure;
            }

            var error = Assert.ThrowsException<IOException>(() => game.CreateComposition("登録編成", Input()));

            Assert.AreSame(failure, error);
            Assert.AreEqual(1, game.compositions.Count);
            Assert.AreSame(stock, game.compositions[0]);
            Assert.AreEqual((money, 3), (game.Money, stock.HeldUnits));
        }

        [TestMethod]
        public void CreationKeepsTheRegistrationListEvaluatedBeforeInputEnumeration()
        {
            var game = Game();
            var original = game.compositions;
            var replacement = new List<IComposition>();
            var car = new Car { bestSpeed = 60, power = PowerEnum.Steam, gauge = CarGaugeEnum.Narrow, type = RailTypeEnum.Iron, seat = SeatEnum.Semi };
            game.vehicles.Add(car);
            IEnumerable<KeyValuePair<Car, int>> Input()
            {
                // 公開fieldが入力列挙中に置換されても、旧VM式の登録先評価順を保つ。
                game.compositions = replacement;
                yield return new(car, 1);
            }

            var result = game.CreateComposition("登録編成", Input());

            Assert.AreSame(replacement, game.compositions);
            Assert.AreEqual(0, replacement.Count);
            Assert.AreEqual(1, original.Count);
            Assert.AreSame(result, original[0]);
        }
    }
}
