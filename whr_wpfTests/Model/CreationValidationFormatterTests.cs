using whr_wpf.View;
using System.Reflection;
using System.Windows.Input;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class CreationValidationFormatterTests
    {
        [DataTestMethod]
        [DataRow(VehicleCreationReason.None, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.None, "")]
        [DataRow(VehicleCreationReason.MissingName, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.None, "名無しの権兵衛です")]
        [DataRow(VehicleCreationReason.EngineUnavailable, PowerEnum.LinearMotor, SeatEnum.None, CarTiltEnum.None, "リニアは作れません")]
        [DataRow(VehicleCreationReason.EngineUnavailable, PowerEnum.Electricity, SeatEnum.None, CarTiltEnum.None, "電車は作れません")]
        [DataRow(VehicleCreationReason.EngineUnavailable, PowerEnum.Diesel, SeatEnum.None, CarTiltEnum.None, "ディーゼルは作れません")]
        [DataRow(VehicleCreationReason.SpeedExceeded, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.None, "速すぎます")]
        [DataRow(VehicleCreationReason.SteamExpired, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.None, "時代遅れです")]
        [DataRow(VehicleCreationReason.FreeGaugeUnavailable, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.None, "フリーゲージは作れません")]
        [DataRow(VehicleCreationReason.SeatUnavailable, PowerEnum.Steam, SeatEnum.Dual, CarTiltEnum.None, "デュアル不可")]
        [DataRow(VehicleCreationReason.SeatUnavailable, PowerEnum.Steam, SeatEnum.Convertible, CarTiltEnum.None, "転換式クロス不可")]
        [DataRow(VehicleCreationReason.SeatUnavailable, PowerEnum.Steam, SeatEnum.RetructableLong, CarTiltEnum.None, "収納式ロング不可")]
        [DataRow(VehicleCreationReason.SeatUnavailable, PowerEnum.Steam, SeatEnum.Rich, CarTiltEnum.None, "豪華クロス不可")]
        [DataRow(VehicleCreationReason.SeatUnavailable, PowerEnum.Steam, SeatEnum.DoubleDeckerRich, CarTiltEnum.None, "豪華クロス不可")]
        [DataRow(VehicleCreationReason.TiltUnavailable, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.Pendulum, "振り子式車体傾斜装置は未開発")]
        [DataRow(VehicleCreationReason.TiltUnavailable, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.SimpleMecha, "機械式式車体傾斜装置は未開発")]
        [DataRow(VehicleCreationReason.TiltUnavailable, PowerEnum.Steam, SeatEnum.None, CarTiltEnum.HighMecha, "機械式式車体傾斜装置は未開発")]
        public void VehicleMessagesKeepExistingText(VehicleCreationReason reason, PowerEnum power, SeatEnum seat, CarTiltEnum tilt, string expected)
            => Assert.AreEqual(expected, CreationValidationFormatter.Format(Vehicle(reason, power, seat, tilt)));

        [DataTestMethod]
        [DataRow(CompositionCreationReason.None, null)]
        [DataRow(CompositionCreationReason.MissingName, "名前が指定されていません")]
        [DataRow(CompositionCreationReason.NoVehicles, "車両の指定がありません")]
        [DataRow(CompositionCreationReason.GaugeMismatch, "車両の軌間に違いがあります")]
        [DataRow(CompositionCreationReason.TrackTypeMismatch, "車両の軌道タイプに違いがあります")]
        [DataRow(CompositionCreationReason.PowerMismatch, "車両の動力に違いがあります")]
        [DataRow(CompositionCreationReason.TiltMismatch, "車両の車体傾斜装置に違いがあります")]
        [DataRow(CompositionCreationReason.SpeedTooLow, "最高速度が40km/hに達していません")]
        public void CompositionMessagesKeepExistingText(CompositionCreationReason reason, string? expected)
            => Assert.AreEqual(expected, CreationValidationFormatter.Format(new CompositionCreationCheck(reason)));

        [DataTestMethod]
        [DataRow(VehicleCreationReason.NegativeSpeed, "速度は0以上で指定してください")]
        [DataRow(VehicleCreationReason.UndefinedPower, "動力の指定が不正です")]
        [DataRow(VehicleCreationReason.UndefinedGauge, "軌間の指定が不正です")]
        [DataRow(VehicleCreationReason.UndefinedSeat, "座席の指定が不正です")]
        [DataRow(VehicleCreationReason.UndefinedTilt, "車体傾斜装置の指定が不正です")]
        public void VehicleInputRejectionUsesTheSameMessageAtCheckAndExecution(VehicleCreationReason reason, string message)
        {
            var check = Vehicle(reason);
            Assert.AreEqual(message, CreationValidationFormatter.Format(check));
            Assert.AreEqual(message, CreationValidationFormatter.Format(new VehicleDevelopmentRejectedException(check)));
        }

        [DataTestMethod]
        [DataRow(CompositionCreationReason.NegativeQuantity, "両数は0以上で指定してください")]
        [DataRow(CompositionCreationReason.QuantityExceeded, "1車種の両数は16両以下で指定してください")]
        [DataRow(CompositionCreationReason.NegativeVehicleSpeed, "負の速度の車両は使用できません")]
        [DataRow(CompositionCreationReason.UndefinedGauge, "車両の軌間の指定が不正です")]
        [DataRow(CompositionCreationReason.UndefinedTrackType, "車両の軌道タイプの指定が不正です")]
        [DataRow(CompositionCreationReason.UndefinedPower, "車両の動力の指定が不正です")]
        [DataRow(CompositionCreationReason.UndefinedSeat, "車両の座席の指定が不正です")]
        [DataRow(CompositionCreationReason.UndefinedTilt, "車両の車体傾斜装置の指定が不正です")]
        [DataRow(CompositionCreationReason.UnregisteredVehicle, "ゲームに登録されていない車両は使用できません")]
        public void CompositionInputRejectionHasAnActionableMessage(CompositionCreationReason reason, string message)
        {
            var check = new CompositionCreationCheck(reason) { QuantityLimit = 16 };
            Assert.AreEqual(message, CreationValidationFormatter.Format(check));
            Assert.AreEqual(message, CreationValidationFormatter.Format(new CompositionCreationRejectedException(check)));
        }

        [TestMethod]
        public void ExecutionMessagesKeepDetailedAndGenericDistinction()
        {
            var vehicle = Vehicle(VehicleCreationReason.MissingName);
            Assert.AreEqual("名無しの権兵衛です", CreationValidationFormatter.Format(vehicle));
            Assert.AreEqual("車両を開発可能な技術が揃っていません",
                CreationValidationFormatter.Format(new VehicleDevelopmentRejectedException(vehicle)));
            var composition = new CompositionCreationCheck(CompositionCreationReason.MissingName);
            Assert.AreEqual(CreationValidationFormatter.Format(composition),
                CreationValidationFormatter.Format(new CompositionCreationRejectedException(composition)));
        }

        [TestMethod]
        public void UnknownReasonsAndUnsupportedTargetsAreNotSilentlyDisplayedAsSuccess()
        {
            foreach (var check in new[]
            {
                Vehicle((VehicleCreationReason)999),
                Vehicle(VehicleCreationReason.EngineUnavailable, PowerEnum.Steam),
                Vehicle(VehicleCreationReason.SeatUnavailable, seat: SeatEnum.None),
                Vehicle(VehicleCreationReason.TiltUnavailable, tilt: CarTiltEnum.None)
            })
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => CreationValidationFormatter.Format(check));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                CreationValidationFormatter.Format(new CompositionCreationCheck((CompositionCreationReason)999)));
        }

        [TestMethod]
        public void VehicleViewModelConnectsMessagesAndCommandAvailability()
        {
            var game = Game();
            game.genkaiJoki = 60;
            var vm = CreateViewModel("VehicleDevelopViewModel", game);
            Set(vm, "Name", "試験");
            Set(vm, "BestSpeed", 61);
            Assert.AreEqual("速すぎます", Get<string>(vm, "Msg"));
            Assert.IsFalse(Get<ICommand>(vm, "Kettei").CanExecute(null));
            Set(vm, "BestSpeed", 60);
            Assert.IsTrue(Get<ICommand>(vm, "Kettei").CanExecute(null));
            StringAssert.StartsWith(Get<string>(vm, "Msg"), "車両価格　");
            Set(vm, "Name", "");
            Assert.AreEqual("名無しの権兵衛です", Get<string>(vm, "Msg"));
            Assert.IsFalse(Get<ICommand>(vm, "Kettei").CanExecute(null));
        }

        [TestMethod]
        public void CompositionViewModelKeepsEmptySelectionAndValidationConnection()
        {
            var game = Game();
            var car = new Car { bestSpeed = 40, power = PowerEnum.Steam, gauge = CarGaugeEnum.Narrow, type = RailTypeEnum.Iron, seat = SeatEnum.Semi };
            game.vehicles.Add(car);
            var vm = CreateViewModel("CompositionMakeViewModel", game);
            Set(vm, "Name", "試験");
            Assert.AreEqual("", Get<string>(vm, "ErrorMsg"));
            Assert.IsFalse(Get<ICommand>(vm, "Make").CanExecute(null));
            Set(vm, "Vehicle", car);
            Assert.AreEqual("車両の指定がありません", Get<string>(vm, "ErrorMsg"));
            Set(vm, "Quantity", 1);
            Assert.AreEqual("", Get<string>(vm, "ErrorMsg"));
            Assert.IsTrue(Get<ICommand>(vm, "Make").CanExecute(null));
            Set(vm, "Name", "");
            Assert.AreEqual("名前が指定されていません", Get<string>(vm, "ErrorMsg"));
            Assert.IsFalse(Get<ICommand>(vm, "Make").CanExecute(null));
        }

        // テストのために製品の内部VMを公開せず、公開プロパティとコマンドの接続を確認する。
        private static object CreateViewModel(string name, GameInfo game)
            => Activator.CreateInstance(typeof(whr_wpf.ViewModel.ViewModelBase).Assembly.GetType($"whr_wpf.ViewModel.Vehicle.{name}")!, game, null)!;
        private static void Set(object vm, string name, object value)
            => vm.GetType().GetProperty(name)!.SetValue(vm, value);
        private static T Get<T>(object vm, string name)
            => (T)vm.GetType().GetProperty(name)!.GetValue(vm)!;
        private static VehicleCreationCheck Vehicle(VehicleCreationReason reason, PowerEnum power = PowerEnum.Steam,
            SeatEnum seat = SeatEnum.None, CarTiltEnum tilt = CarTiltEnum.None)
            => new(reason, power, 60, 40, CarGaugeEnum.Narrow, seat, tilt, 1880, 1970);
    }
}
