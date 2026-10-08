using System.Collections;
using whr_wpf.Util;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class CreationInputContractTests
    {
        [DataTestMethod]
        [DataRow(VehicleCreationReason.NegativeSpeed)]
        [DataRow(VehicleCreationReason.UndefinedPower)]
        [DataRow(VehicleCreationReason.UndefinedGauge)]
        [DataRow(VehicleCreationReason.UndefinedSeat)]
        [DataRow(VehicleCreationReason.UndefinedTilt)]
        public void VehicleInputRejectionPrecedesPaymentAndLeavesStateUnchanged(VehicleCreationReason reason)
        {
            var game = Game();
            int speed = reason == VehicleCreationReason.NegativeSpeed ? -1 : 40;
            var power = reason == VehicleCreationReason.UndefinedPower ? (PowerEnum)999 : PowerEnum.Steam;
            var gauge = reason == VehicleCreationReason.UndefinedGauge ? (CarGaugeEnum)999 : CarGaugeEnum.Narrow;
            // 座席0も未定義。SeatEnum.Noneの1と区別する。
            var seat = reason == VehicleCreationReason.UndefinedSeat ? (SeatEnum)0 : SeatEnum.Semi;
            var tilt = reason == VehicleCreationReason.UndefinedTilt ? (CarTiltEnum)999 : CarTiltEnum.None;
            int notifications = 0;
            game.PropertyChanged += (_, _) => notifications++;
            var check = game.CheckCreateVehicle("入力", speed, power, gauge, seat, tilt);
            var error = Assert.ThrowsException<VehicleDevelopmentRejectedException>(() =>
                game.DevelopVehicle("入力", speed, power, gauge, seat, tilt));
            Assert.AreEqual(reason, check.Reason);
            Assert.AreEqual(check, error.Check);
            Assert.AreEqual((1000000L, 0, 0, 0), (game.Money, game.outlay, game.vehicles.Count, notifications));
        }

        [TestMethod]
        public void VehicleInputPriorityKeepsMissingNameFirstAndZeroSpeedAllowed()
        {
            var game = Game();
            Assert.AreEqual(VehicleCreationReason.MissingName,
                game.CheckCreateVehicle("", -1, (PowerEnum)999, (CarGaugeEnum)999, (SeatEnum)0, (CarTiltEnum)999).Reason);
            Assert.AreEqual(VehicleCreationReason.NegativeSpeed,
                game.CheckCreateVehicle("入力", -1, (PowerEnum)999, CarGaugeEnum.Narrow, SeatEnum.None, CarTiltEnum.None).Reason);
            Assert.IsTrue(game.CheckCreateVehicle("入力", 0, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.None, CarTiltEnum.None).CanCreateVehicle);
        }

        [TestMethod]
        public void EveryDefinedVehicleEnumRemainsSupportedWhenTechnologyIsAvailable()
        {
            var game = Game();
            game.genkaiJoki = game.genkaiDenki = game.genkaiKidosha = game.genkaiLinear = 60;
            game.isDevelopedFreeGauge = game.isDevelopedDualSeat = game.isDevelopedConvertibleCross =
                game.isDevelopedRetructableLong = game.isDevelopedRichCross =
                game.isDevelopedCarTiltPendulum = game.isDevelopedMachineTilt = true;
            foreach (var power in Enum.GetValues<PowerEnum>())
                Assert.IsTrue(game.CheckCreateVehicle("入力", 40, power, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None).CanCreateVehicle);
            foreach (var gauge in Enum.GetValues<CarGaugeEnum>())
                Assert.IsTrue(game.CheckCreateVehicle("入力", 40, PowerEnum.Steam, gauge, SeatEnum.Semi, CarTiltEnum.None).CanCreateVehicle);
            foreach (var seat in Enum.GetValues<SeatEnum>())
                Assert.IsTrue(game.CheckCreateVehicle("入力", 40, PowerEnum.Steam, CarGaugeEnum.Narrow, seat, CarTiltEnum.None).CanCreateVehicle);
            foreach (var tilt in Enum.GetValues<CarTiltEnum>())
                Assert.IsTrue(game.CheckCreateVehicle("入力", 40, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.Semi, tilt).CanCreateVehicle);
        }

        [DataTestMethod]
        [DataRow(CompositionCreationReason.NegativeQuantity)]
        [DataRow(CompositionCreationReason.QuantityExceeded)]
        [DataRow(CompositionCreationReason.NegativeVehicleSpeed)]
        [DataRow(CompositionCreationReason.UndefinedGauge)]
        [DataRow(CompositionCreationReason.UndefinedTrackType)]
        [DataRow(CompositionCreationReason.UndefinedPower)]
        [DataRow(CompositionCreationReason.UndefinedSeat)]
        [DataRow(CompositionCreationReason.UndefinedTilt)]
        [DataRow(CompositionCreationReason.UnregisteredVehicle)]
        public void CompositionCheckAndCreationRejectTheSameInputWithoutChangingTheGame(CompositionCreationReason reason)
        {
            var game = Game();
            var existing = Stock(game, 3);
            var car = Vehicle();
            if (reason != CompositionCreationReason.UnregisteredVehicle) game.vehicles.Add(car);
            int count = 1;
            switch (reason)
            {
                case CompositionCreationReason.NegativeQuantity: count = -1; break;
                case CompositionCreationReason.QuantityExceeded: count = 17; break;
                case CompositionCreationReason.NegativeVehicleSpeed: car.bestSpeed = -40; break;
                case CompositionCreationReason.UndefinedGauge: car.gauge = (CarGaugeEnum)999; break;
                case CompositionCreationReason.UndefinedTrackType: car.type = (RailTypeEnum)999; break;
                case CompositionCreationReason.UndefinedPower: car.power = (PowerEnum)999; break;
                case CompositionCreationReason.UndefinedSeat: car.seat = (SeatEnum)0; break;
                case CompositionCreationReason.UndefinedTilt: car.carTilt = (CarTiltEnum)999; break;
            }
            var rows = Rows((car, count));
            long money = game.Money;
            int notifications = 0;
            game.PropertyChanged += (_, _) => notifications++;
            var check = game.CheckCreateComposition("入力", rows);
            var error = Assert.ThrowsException<CompositionCreationRejectedException>(() => game.CreateComposition("入力", rows));
            Assert.AreEqual(reason, check.Reason);
            Assert.AreEqual(check, error.Check);
            Assert.AreEqual((money, 3, 1, 0), (game.Money, existing.HeldUnits, game.compositions.Count, notifications));
            Assert.AreSame(existing, game.compositions[0]);
            car.Name = "後で変更";
            Assert.AreEqual("車両", check.CarName);
            if (reason == CompositionCreationReason.NegativeQuantity || reason == CompositionCreationReason.QuantityExceeded)
                Assert.AreEqual(count, check.RequestedQuantity);
            if (reason == CompositionCreationReason.QuantityExceeded) Assert.AreEqual(16, check.QuantityLimit);
            if (reason == CompositionCreationReason.UndefinedSeat) Assert.AreEqual(0, check.InvalidValue);
        }

        [TestMethod]
        public void ZeroSelectionDoesNotValidateUnusedCarAndSixteenIsPerVehicleType()
        {
            var game = Game();
            var first = Vehicle();
            var second = Vehicle();
            var unused = new Car { Name = "未使用", bestSpeed = -1, power = (PowerEnum)999 };
            game.vehicles.AddRange(new[] { first, second });
            var result = game.CreateComposition("入力", Rows((first, 16), (second, 16), (unused, 0)));
            Assert.AreEqual(32, result.CarCount);
            Assert.AreEqual(2, result.Vehicles.Count);
            Assert.AreEqual(CompositionCreationReason.NoVehicles, game.CheckCreateComposition("入力", Rows((unused, 0))).Reason);
        }

        [TestMethod]
        public void OwnershipUsesReferenceIdentityAndIsRecheckedAfterAnAcceptedQuery()
        {
            var game = Game();
            var car = Vehicle();
            game.vehicles.Add(car);
            var sameFeatures = Vehicle();
            Assert.AreEqual(CompositionCreationReason.UnregisteredVehicle, game.CheckCreateComposition("入力", Rows((sameFeatures, 1))).Reason);
            var accepted = game.CheckCreateComposition("入力", Rows((car, 1)));
            Assert.IsTrue(accepted.CanCompositionMake);
            game.vehicles.Remove(car);
            var error = Assert.ThrowsException<CompositionCreationRejectedException>(() => game.CreateComposition("入力", Rows((car, 1))));
            Assert.AreEqual(CompositionCreationReason.UnregisteredVehicle, error.Check.Reason);
            Assert.IsTrue(accepted.CanCompositionMake);
            Assert.AreEqual(0, game.compositions.Count);
        }

        [TestMethod]
        public void CreationRechecksQuantityAndAttributesChangedAfterAnAcceptedQuery()
        {
            var game = Game();
            var car = Vehicle();
            game.vehicles.Add(car);
            var rows = new Dictionary<Car, int> { [car] = 16 };
            Assert.IsTrue(game.CheckCreateComposition("入力", rows).CanCompositionMake);
            rows[car] = 17;
            Assert.AreEqual(CompositionCreationReason.QuantityExceeded,
                Assert.ThrowsException<CompositionCreationRejectedException>(() => game.CreateComposition("入力", rows)).Check.Reason);
            rows[car] = 16;
            car.power = (PowerEnum)999;
            Assert.AreEqual(CompositionCreationReason.UndefinedPower,
                Assert.ThrowsException<CompositionCreationRejectedException>(() => game.CreateComposition("入力", rows)).Check.Reason);
            Assert.AreEqual(0, game.compositions.Count);
        }

        [TestMethod]
        public void FactoryAndGameCreateFromTheInputTheyValidatedWithoutReenumeratingIt()
        {
            var car = Vehicle();
            var other = Vehicle();
            other.power = (PowerEnum)999;
            var factoryInput = new ChangingRows(Rows((car, 16)), Rows((other, 17)));
            var factoryResult = CompositionFactory.CreateComposition("入力", factoryInput);
            Assert.AreEqual(1, factoryInput.EnumerationCount);
            Assert.AreEqual(16, factoryResult.Vehicles[car]);
            var game = Game();
            game.vehicles.Add(car);
            var gameInput = new ChangingRows(Rows((car, 16)), Rows((other, 17)));
            var gameResult = game.CreateComposition("入力", gameInput);
            Assert.AreEqual(1, gameInput.EnumerationCount);
            Assert.AreEqual(16, gameResult.Vehicles[car]);
            Assert.AreSame(gameResult, game.compositions.Single());
        }

        [TestMethod]
        public void CompositionReasonPriorityDoesNotDependOnWhichCarIsEnumeratedFirst()
        {
            var first = Vehicle();
            var second = Vehicle();
            first.power = (PowerEnum)999;
            second.gauge = (CarGaugeEnum)999;
            foreach (var rows in new[] { Rows((first, 1), (second, 1)), Rows((second, 1), (first, 1)) })
                Assert.AreEqual(CompositionCreationReason.UndefinedGauge, CompositionFactory.CheckMakeComposition("入力", rows).Reason);
            Assert.AreEqual(CompositionCreationReason.MissingName, CompositionFactory.CheckMakeComposition("", Rows((first, -1))).Reason);
            Assert.AreEqual(CompositionCreationReason.NegativeQuantity, CompositionFactory.CheckMakeComposition("入力", Rows((first, 17), (second, -1))).Reason);
        }

        [TestMethod]
        public void ScenarioInitialCompositionIsNotSubjectToNewCreationLimit()
        {
            var game = Game();
            var initial = new DefautltComposition { Name = "初期編成", CarCount = 17 };
            game.SelectedMode.DefautltCompositions.Add(initial);
            game.SelectedMode = game.SelectedMode;
            Assert.AreSame(initial, game.compositions.Single());
            Assert.AreEqual(17, initial.CarCount);
            Assert.AreEqual(0, game.vehicles.Count);
        }

        private static Car Vehicle() => new() { Name = "車両", bestSpeed = 40,
            gauge = CarGaugeEnum.Narrow, type = RailTypeEnum.Iron, power = PowerEnum.Steam,
            seat = SeatEnum.Semi, carTilt = CarTiltEnum.None };
        private static KeyValuePair<Car, int>[] Rows(params (Car Car, int Count)[] rows)
            => rows.Select(row => new KeyValuePair<Car, int>(row.Car, row.Count)).ToArray();

        private sealed class ChangingRows(KeyValuePair<Car, int>[] first, KeyValuePair<Car, int>[] later) : IEnumerable<KeyValuePair<Car, int>>
        {
            public int EnumerationCount { get; private set; }
            public IEnumerator<KeyValuePair<Car, int>> GetEnumerator()
                => ((IEnumerable<KeyValuePair<Car, int>>)(++EnumerationCount == 1 ? first : later)).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
