using whr_wpf.Util;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class VehicleValidationBehaviorTests
    {
        [DataTestMethod]
        [DataRow(PowerEnum.Steam)]
        [DataRow(PowerEnum.Electricity)]
        [DataRow(PowerEnum.Diesel)]
        [DataRow(PowerEnum.LinearMotor)]
        public void EngineSpeedLimitIncludesBoundary(PowerEnum power)
        {
            var game = Game();
            game.genkaiJoki = game.genkaiDenki = game.genkaiKidosha = game.genkaiLinear = 60;
            Assert.AreEqual(VehicleCreationReason.None, Check(game, speed: 60, power: power));
            Assert.AreEqual(VehicleCreationReason.SpeedExceeded, Check(game, speed: 61, power: power));
            var failure = game.CheckCreateVehicle("車両", 61, power, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None);
            Assert.AreEqual((power, 61, 60, false), (failure.Power, failure.RequestedSpeed, failure.SpeedLimit, failure.CanCreateVehicle));
        }

        [DataTestMethod]
        [DataRow(PowerEnum.Electricity)]
        [DataRow(PowerEnum.Diesel)]
        [DataRow(PowerEnum.LinearMotor)]
        public void UnavailableEnginePrecedesSpeedError(PowerEnum power)
            => Assert.AreEqual(VehicleCreationReason.EngineUnavailable, Check(Game(), speed: 999, power: power));

        [DataTestMethod]
        [DataRow(SeatEnum.Dual)]
        [DataRow(SeatEnum.Convertible)]
        [DataRow(SeatEnum.RetructableLong)]
        [DataRow(SeatEnum.Rich)]
        [DataRow(SeatEnum.DoubleDeckerRich)]
        public void SeatRequiresItsTechnology(SeatEnum seat)
        {
            var game = Game();
            Assert.AreEqual(VehicleCreationReason.SeatUnavailable, Check(game, seat: seat));
            game.isDevelopedDualSeat = game.isDevelopedConvertibleCross =
                game.isDevelopedRetructableLong = game.isDevelopedRichCross = true;
            Assert.AreEqual(VehicleCreationReason.None, Check(game, seat: seat));
        }

        [DataTestMethod]
        [DataRow(CarTiltEnum.Pendulum)]
        [DataRow(CarTiltEnum.SimpleMecha)]
        [DataRow(CarTiltEnum.HighMecha)]
        public void TiltRequiresItsTechnology(CarTiltEnum tilt)
        {
            var game = Game();
            Assert.AreEqual(VehicleCreationReason.TiltUnavailable, Check(game, tilt: tilt));
            game.isDevelopedCarTiltPendulum = game.isDevelopedMachineTilt = true;
            Assert.AreEqual(VehicleCreationReason.None, Check(game, tilt: tilt));
        }

        [TestMethod]
        public void VehicleErrorsFollowCurrentPriority()
        {
            var game = Game();
            // 複数条件を同時に違反させ、先行条件の解消で次の案内へ進むことを固定する。
            Assert.AreEqual(VehicleCreationReason.MissingName, Check(game, name: " \t", power: PowerEnum.LinearMotor, gauge: CarGaugeEnum.FreeGauge));
            Assert.AreEqual(VehicleCreationReason.EngineUnavailable, Check(game, power: PowerEnum.LinearMotor, gauge: CarGaugeEnum.FreeGauge));
            game.genkaiLinear = 39;
            Assert.AreEqual(VehicleCreationReason.SpeedExceeded, Check(game, power: PowerEnum.LinearMotor, gauge: CarGaugeEnum.FreeGauge));
            game.SteamYear = game.Year;
            Assert.AreEqual(VehicleCreationReason.FreeGaugeUnavailable, Check(game, gauge: CarGaugeEnum.FreeGauge));
            game.isDevelopedFreeGauge = true;
            Assert.AreEqual(VehicleCreationReason.SteamExpired, Check(game, speed: 999, gauge: CarGaugeEnum.FreeGauge));
            game.SteamYear = game.Year + 1;
            Assert.AreEqual(VehicleCreationReason.None, Check(game, gauge: CarGaugeEnum.FreeGauge));
            Assert.AreEqual(VehicleCreationReason.SpeedExceeded, Check(game, speed: 999, seat: SeatEnum.Dual, tilt: CarTiltEnum.Pendulum));
            Assert.AreEqual(VehicleCreationReason.SeatUnavailable, Check(game, seat: SeatEnum.Dual, tilt: CarTiltEnum.Pendulum));
        }

        [TestMethod]
        public void VehicleCheckCurrentlyAllowsNegativeSpeedAndUndefinedEnums()
        {
            Assert.AreEqual(VehicleCreationReason.None, Check(Game(), speed: -1));
            Assert.AreEqual(VehicleCreationReason.None, Check(Game(), speed: 999, power: (PowerEnum)999,
                gauge: (CarGaugeEnum)999, seat: (SeatEnum)999, tilt: (CarTiltEnum)999));
        }

        [TestMethod]
        public void DevelopmentFailureDistinguishesValidationMoneyAndInternalErrors()
        {
            var game = Game();
            game.Money = 0;
            var rejection = Assert.ThrowsException<VehicleDevelopmentRejectedException>(() =>
                game.DevelopVehicle("", 40, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None));
            Assert.AreEqual(VehicleCreationReason.MissingName, rejection.Check.Reason);
            Assert.ThrowsException<MoneyShortException>(() =>
                game.DevelopVehicle("車両", 40, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None));
            // 蒸気の限界速度0・指定速度0は検査を通るが費用計算で失敗する。
            game.genkaiJoki = 0;
            Assert.AreEqual(VehicleCreationReason.None, Check(game, speed: 0));
            Assert.ThrowsException<DivideByZeroException>(() =>
                game.DevelopVehicle("車両", 0, PowerEnum.Steam, CarGaugeEnum.Narrow, SeatEnum.Semi, CarTiltEnum.None));
            Assert.AreEqual((0L, 0, 0), (game.Money, game.outlay, game.vehicles.Count));
        }

        [TestMethod]
        public void LinearDevelopmentRegistersRequestedFeaturesAndChargesOnce()
        {
            var game = Game();
            game.genkaiLinear = 60;
            game.isDevelopedFreeGauge = game.isDevelopedRichCross = game.isDevelopedMachineTilt = true;
            int cost = game.CalcDevelopVehicleCost(60, PowerEnum.LinearMotor, CarGaugeEnum.FreeGauge, SeatEnum.Rich, CarTiltEnum.HighMecha);
            game.DevelopVehicle("リニア", 60, PowerEnum.LinearMotor, CarGaugeEnum.FreeGauge, SeatEnum.Rich, CarTiltEnum.HighMecha);
            var car = game.vehicles.Single();
            Assert.AreEqual(("リニア", 60, PowerEnum.LinearMotor, CarGaugeEnum.FreeGauge, SeatEnum.Rich, CarTiltEnum.HighMecha, RailTypeEnum.LinearMotor),
                (car.Name, car.bestSpeed, car.power, car.gauge, car.seat, car.carTilt, car.type));
            // 開発費は資金から減るが、現状のSpendMoneyは週次支出へ計上しない。
            Assert.AreEqual((1000000L - cost, 0, cost / 10), (game.Money, game.outlay, car.money));
        }

        [TestMethod]
        public void CompositionErrorsFollowCurrentPriorityAndCreationUsesSameReason()
        {
            var first = Vehicle();
            var second = Vehicle();
            first.bestSpeed = second.bestSpeed = 39;
            second.gauge = CarGaugeEnum.Regular;
            second.type = RailTypeEnum.LinearMotor;
            second.power = PowerEnum.Electricity;
            second.carTilt = CarTiltEnum.Pendulum;
            var rows = Rows((first, 1), (second, 1));
            AssertCompositionError(" \t", rows, CompositionCreationReason.MissingName);
            AssertCompositionError("編成", Rows((first, 0), (second, -1)), CompositionCreationReason.NoVehicles);
            AssertCompositionError("編成", rows, CompositionCreationReason.GaugeMismatch);
            second.gauge = first.gauge;
            AssertCompositionError("編成", rows, CompositionCreationReason.TrackTypeMismatch);
            second.type = first.type;
            AssertCompositionError("編成", rows, CompositionCreationReason.PowerMismatch);
            second.power = first.power;
            AssertCompositionError("編成", rows, CompositionCreationReason.TiltMismatch);
            second.carTilt = first.carTilt;
            AssertCompositionError("編成", rows, CompositionCreationReason.SpeedTooLow);
            Assert.AreEqual((1, 1, 39, 39), (rows[0].Value, rows[1].Value, first.bestSpeed, second.bestSpeed));
        }

        [DataTestMethod]
        [DataRow(CarGaugeEnum.Narrow)]
        [DataRow(CarGaugeEnum.Regular)]
        [DataRow(CarGaugeEnum.FreeGauge)]
        public void FreeGaugeCombinesWithOneOrdinaryGauge(CarGaugeEnum gauge)
        {
            var ordinary = Vehicle();
            ordinary.gauge = gauge;
            var free = Vehicle();
            free.gauge = CarGaugeEnum.FreeGauge;
            var ignored = Vehicle();
            ignored.type = RailTypeEnum.LinearMotor;
            var rows = Rows((ordinary, 2), (free, 1), (ignored, 0), (Vehicle(), -3));
            Assert.AreEqual(CompositionCreationReason.None, CompositionFactory.CheckMakeComposition("編成", rows).Reason);
            var composition = CompositionFactory.CreateComposition("編成", rows);
            Assert.AreEqual(("編成", gauge, RailTypeEnum.Iron, PowerEnum.Steam, CarTiltEnum.None, 40),
                (composition.Name, composition.Gauge, composition.Type, composition.Power, composition.Tilt, composition.BestSpeed));
            Assert.AreEqual(2, composition.Vehicles.Count);
            Assert.AreEqual(2, composition.Vehicles[ordinary]);
            Assert.AreEqual(1, composition.Vehicles[free]);
        }

        [DataTestMethod]
        [DataRow(38, false)]
        [DataRow(39, true)]
        public void WeightedRootMeanSquareSpeedIsTruncatedAtForty(int lowSpeed, bool allowed)
        {
            var low = Vehicle();
            var high = Vehicle();
            low.bestSpeed = lowSpeed;
            high.bestSpeed = 41;
            var rows = Rows((low, 2), (high, 3));
            Assert.AreEqual(allowed, CompositionFactory.CheckMakeComposition("編成", rows).CanCompositionMake);
            if (allowed) Assert.AreEqual(40, CompositionFactory.CreateComposition("編成", rows).BestSpeed);
        }

        [TestMethod]
        public void CompositionCurrentlyAllowsUndefinedEnumsButPropagatesDuplicateKeyFailure()
        {
            var car = Vehicle();
            car.gauge = (CarGaugeEnum)999;
            car.type = (RailTypeEnum)999;
            car.power = (PowerEnum)999;
            car.carTilt = (CarTiltEnum)999;
            var rows = Rows((car, 1));
            Assert.AreEqual(CompositionCreationReason.None, CompositionFactory.CheckMakeComposition("編成", rows).Reason);
            Assert.AreEqual((CarGaugeEnum)999, CompositionFactory.CreateComposition("編成", rows).Gauge);
            var duplicates = Rows((car, 1), (car, 2));
            Assert.ThrowsException<ArgumentException>(() => CompositionFactory.CheckMakeComposition("編成", duplicates));
            Assert.ThrowsException<ArgumentException>(() => CompositionFactory.CreateComposition("編成", duplicates));
            Assert.AreEqual(40, car.bestSpeed);
        }

        [TestMethod]
        public void CompositionCurrentlySquaresNegativeSpeedBeforeValidation()
        {
            var car = Vehicle();
            car.bestSpeed = -40;
            var rows = Rows((car, 1));
            // 負速度も二乗される現状を固定し、今回の構造化変更と仕様修正を分離する。
            Assert.AreEqual(CompositionCreationReason.None, CompositionFactory.CheckMakeComposition("編成", rows).Reason);
            Assert.AreEqual(40, CompositionFactory.CreateComposition("編成", rows).BestSpeed);
            Assert.AreEqual(-40, car.bestSpeed);
        }

        [TestMethod]
        public void VehicleCheckCapturesArgumentsAndLimitsBeforeLaterModelChanges()
        {
            var game = Game();
            var check = game.CheckCreateVehicle("車両", 41, PowerEnum.Steam, CarGaugeEnum.Regular,
                SeatEnum.Rich, CarTiltEnum.HighMecha);
            game.genkaiJoki = 100;
            game.SteamYear = 2000;
            Assert.AreEqual((VehicleCreationReason.SpeedExceeded, PowerEnum.Steam, 41, 40,
                CarGaugeEnum.Regular, SeatEnum.Rich, CarTiltEnum.HighMecha, 1880, 1970),
                (check.Reason, check.Power, check.RequestedSpeed, check.SpeedLimit,
                check.Gauge, check.Seat, check.Tilt, check.Year, check.SteamEndYear));
            Assert.IsFalse(check.CanCreateVehicle);
        }

        [TestMethod]
        public void DevelopmentRechecksTechnologyAtExecutionAndPreservesStateOnRejection()
        {
            var game = Game();
            game.isDevelopedFreeGauge = true;
            var prior = game.CheckCreateVehicle("車両", 40, PowerEnum.Steam, CarGaugeEnum.FreeGauge, SeatEnum.Semi, CarTiltEnum.None);
            Assert.IsTrue(prior.CanCreateVehicle);
            game.isDevelopedFreeGauge = false;
            var rejection = Assert.ThrowsException<VehicleDevelopmentRejectedException>(() =>
                game.DevelopVehicle("車両", 40, PowerEnum.Steam, CarGaugeEnum.FreeGauge, SeatEnum.Semi, CarTiltEnum.None));
            Assert.AreEqual(VehicleCreationReason.FreeGaugeUnavailable, rejection.Check.Reason);
            Assert.IsTrue(prior.CanCreateVehicle);
            Assert.AreEqual((1000000L, 0, 0), (game.Money, game.outlay, game.vehicles.Count));
        }

        [TestMethod]
        public void CompositionMismatchCapturesValuesWithoutReadingLaterStages()
        {
            var first = Vehicle();
            var second = Vehicle();
            second.gauge = CarGaugeEnum.Regular;
            second.type = RailTypeEnum.LinearMotor;
            var check = CompositionFactory.CheckMakeComposition("編成", Rows((first, 1), (second, 1)));
            second.gauge = CarGaugeEnum.FreeGauge;
            first.gauge = CarGaugeEnum.Regular;
            CollectionAssert.AreEquivalent(new[] { CarGaugeEnum.Narrow, CarGaugeEnum.Regular }, check.Gauges.ToArray());
            Assert.AreEqual(CompositionCreationReason.GaugeMismatch, check.Reason);
            Assert.IsFalse(check.CanCompositionMake);
            Assert.IsTrue(check.TrackTypes.IsEmpty);
            Assert.IsTrue(check.Powers.IsEmpty);
            Assert.IsTrue(check.Tilts.IsEmpty);
            Assert.IsNull(check.ActualSpeed);
            Assert.IsNull(check.MinimumSpeed);
        }

        [TestMethod]
        public void CompositionSpeedSnapshotAndCreationReflectTheirOwnCheckTime()
        {
            var car = Vehicle();
            car.bestSpeed = 39;
            var rows = Rows((car, 1));
            var rejected = CompositionFactory.CheckMakeComposition("編成", rows);
            Assert.AreEqual((CompositionCreationReason.SpeedTooLow, (int?)39, (int?)40),
                (rejected.Reason, rejected.ActualSpeed, rejected.MinimumSpeed));
            car.bestSpeed = 40;
            var accepted = CompositionFactory.CheckMakeComposition("編成", rows);
            Assert.IsTrue(accepted.CanCompositionMake);
            car.bestSpeed = 38;
            var execution = Assert.ThrowsException<CompositionCreationRejectedException>(() => CompositionFactory.CreateComposition("編成", rows));
            Assert.AreEqual((CompositionCreationReason.SpeedTooLow, (int?)38, (int?)40),
                (execution.Check.Reason, execution.Check.ActualSpeed, execution.Check.MinimumSpeed));
            Assert.AreEqual(39, rejected.ActualSpeed);
            Assert.IsTrue(accepted.CanCompositionMake);
            Assert.AreEqual((int?)40, accepted.ActualSpeed);
            Assert.AreEqual((1, 38), (rows[0].Value, car.bestSpeed));
        }

        [DataTestMethod]
        [DataRow(CompositionCreationReason.TrackTypeMismatch)]
        [DataRow(CompositionCreationReason.PowerMismatch)]
        [DataRow(CompositionCreationReason.TiltMismatch)]
        public void CompositionMismatchSnapshotsOnlyItsComparedValues(CompositionCreationReason reason)
        {
            var first = Vehicle();
            var second = Vehicle();
            if (reason == CompositionCreationReason.TrackTypeMismatch) second.type = RailTypeEnum.LinearMotor;
            if (reason == CompositionCreationReason.PowerMismatch) second.power = PowerEnum.Electricity;
            if (reason == CompositionCreationReason.TiltMismatch) second.carTilt = CarTiltEnum.Pendulum;
            var check = CompositionFactory.CheckMakeComposition("編成", Rows((first, 1), (second, 1)));
            second.type = first.type;
            second.power = first.power;
            second.carTilt = first.carTilt;
            Assert.AreEqual(reason, check.Reason);
            Assert.IsTrue(check.Gauges.IsEmpty);
            Assert.IsNull(check.ActualSpeed);
            Assert.IsNull(check.MinimumSpeed);
            if (reason == CompositionCreationReason.TrackTypeMismatch)
                CollectionAssert.AreEquivalent(new[] { RailTypeEnum.Iron, RailTypeEnum.LinearMotor }, check.TrackTypes.ToArray());
            else Assert.IsTrue(check.TrackTypes.IsEmpty);
            if (reason == CompositionCreationReason.PowerMismatch)
                CollectionAssert.AreEquivalent(new[] { PowerEnum.Steam, PowerEnum.Electricity }, check.Powers.ToArray());
            else Assert.IsTrue(check.Powers.IsEmpty);
            if (reason == CompositionCreationReason.TiltMismatch)
                CollectionAssert.AreEquivalent(new[] { CarTiltEnum.None, CarTiltEnum.Pendulum }, check.Tilts.ToArray());
            else Assert.IsTrue(check.Tilts.IsEmpty);
        }

        [TestMethod]
        public void MissingCompositionNameDoesNotEnumerateBrokenInput()
        {
            Assert.AreEqual(CompositionCreationReason.MissingName, CompositionFactory.CheckMakeComposition(" \t", BrokenRows()).Reason);
            var exception = Assert.ThrowsException<CompositionCreationRejectedException>(() => CompositionFactory.CreateComposition("", BrokenRows()));
            Assert.AreEqual(CompositionCreationReason.MissingName, exception.Check.Reason);
            Assert.IsTrue(exception.Check.Gauges.IsEmpty);
            Assert.IsNull(exception.Check.ActualSpeed);
        }

        [TestMethod]
        public void InvalidCompositionInputPropagatesOriginalExceptions()
        {
            // 名前以外の内部異常を業務上の拒否へ変換しない。
            Assert.ThrowsException<ArgumentNullException>(() => CompositionFactory.CheckMakeComposition("編成", Rows((null!, 1))));
            Assert.ThrowsException<ArgumentNullException>(() => CompositionFactory.CreateComposition("編成", Rows((null!, 1))));
            var failure = Assert.ThrowsException<FormatException>(() => CompositionFactory.CheckMakeComposition("編成", BrokenRows()));
            Assert.AreEqual("列挙失敗", failure.Message);
            Assert.ThrowsException<FormatException>(() => CompositionFactory.CreateComposition("編成", BrokenRows()));
        }

        private static IEnumerable<KeyValuePair<Car, int>> BrokenRows()
        {
            yield return new KeyValuePair<Car, int>(Vehicle(), 1);
            throw new FormatException("列挙失敗");
        }

        private static VehicleCreationReason Check(GameInfo game, string name = "車両", int speed = 40,
            PowerEnum power = PowerEnum.Steam, CarGaugeEnum gauge = CarGaugeEnum.Narrow,
            SeatEnum seat = SeatEnum.Semi, CarTiltEnum tilt = CarTiltEnum.None)
            => game.CheckCreateVehicle(name, speed, power, gauge, seat, tilt).Reason;

        private static Car Vehicle() => new Car { Name = "車両", bestSpeed = 40,
            gauge = CarGaugeEnum.Narrow, type = RailTypeEnum.Iron, power = PowerEnum.Steam,
            seat = SeatEnum.Semi, carTilt = CarTiltEnum.None };

        private static KeyValuePair<Car, int>[] Rows(params (Car Car, int Count)[] rows)
            => rows.Select(row => new KeyValuePair<Car, int>(row.Car, row.Count)).ToArray();

        private static void AssertCompositionError(string name, KeyValuePair<Car, int>[] rows, CompositionCreationReason reason)
        {
            Assert.AreEqual(reason, CompositionFactory.CheckMakeComposition(name, rows).Reason);
            var exception = Assert.ThrowsException<CompositionCreationRejectedException>(() => CompositionFactory.CreateComposition(name, rows));
            Assert.AreEqual(reason, exception.Check.Reason);
        }
    }
}
