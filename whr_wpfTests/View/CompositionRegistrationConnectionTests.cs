using System.Reflection;
using whr_wpf.Model;
using whr_wpf.ViewModel;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.View.Tests
{
    [TestClass]
    [TestCategory("UiRegression")]
    public class CompositionRegistrationConnectionTests
    {
        [TestMethod]
        public void ConfirmationCallbackRegistersTheViewModelInputThroughTheGameOperation()
        {
            var game = Game();
            var car = new Car { bestSpeed = 60, power = PowerEnum.Steam, gauge = CarGaugeEnum.Narrow, type = RailTypeEnum.Iron, seat = SeatEnum.Semi };
            game.vehicles.Add(car);
            long money = game.Money;
            var type = typeof(ViewModelBase).Assembly.GetType("whr_wpf.ViewModel.Vehicle.CompositionMakeViewModel")!;
            var vm = Activator.CreateInstance(type, game, null)!;
            type.GetProperty("Name")!.SetValue(vm, "画面入力の編成");
            type.GetProperty("Vehicle")!.SetValue(vm, car);
            type.GetProperty("Quantity")!.SetValue(vm, 2);
            var command = type.GetProperty("Make")!.GetValue(vm)!;

            // ダイアログは操作せず、確認承認後に呼ばれる実コールバックへ接続する。
            command.GetType().GetMethod("Make", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(command, null);

            Assert.AreEqual(1, game.compositions.Count);
            var result = (Composition)game.compositions[0];
            Assert.AreEqual("画面入力の編成", result.Name);
            Assert.AreEqual(1, result.Vehicles.Count);
            Assert.AreEqual(2, result.Vehicles[car]);
            Assert.AreEqual((money, 0), (game.Money, result.HeldUnits));
        }
    }
}
