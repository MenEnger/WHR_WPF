using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.ViewModel;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.View.Tests
{
    [TestClass]
    [TestCategory("UiRegression")]
    public class CreationInputConnectionTests
    {
        [TestMethod]
        public void VehicleInvalidExecuteDoesNotCalculateAnEstimateOrOpenConfirmation()
        {
            var game = Game();
            // 未定義動力を蒸気へ訂正した際も、負速度を費用計算より先に拒否する。
            game.genkaiJoki = 0;
            var vm = Create("VehicleDevelopViewModel", game);
            Set(vm, "BestSpeed", -1);
            Set(vm, "Power", (PowerEnum)999);
            Assert.AreEqual("速度は0以上で指定してください", Get<string>(vm, "Msg"));
            var command = Get<ICommand>(vm, "Kettei");
            Assert.IsFalse(command.CanExecute(null));
            int notifications = 0;
            ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => { if (e.PropertyName == "Msg") notifications++; };
            // 直接Executeでも確認前に戻る。回帰してMessageBoxへ進むと待機し得るため、Windowsでは実行時間にも注意する。
            command.Execute(null);
            Assert.AreEqual(1, notifications);
            Assert.AreEqual((1000000L, 0), (game.Money, game.vehicles.Count));
        }

        [TestMethod]
        public void CompositionInvalidQuantityIsDisplayedAndDoesNotReachEstimateOrConfirmation()
        {
            var game = Game();
            var car = Vehicle();
            game.vehicles.Add(car);
            var vm = Create("CompositionMakeViewModel", game);
            Set(vm, "Name", "入力");
            Set(vm, "Vehicle", car);
            var command = Get<ICommand>(vm, "Make");
            foreach (var (quantity, message) in new[] { (-1, "両数は0以上で指定してください"), (17, "1車種の両数は16両以下で指定してください") })
            {
                Set(vm, "Quantity", quantity);
                Assert.AreEqual(message, Get<string>(vm, "ErrorMsg"));
                StringAssert.Contains(Get<string>(vm, "Description"), message);
                Assert.IsFalse(Get<string>(vm, "Description").Contains("編成価格"));
                Assert.IsFalse(command.CanExecute(null));
                command.Execute(null);
            }
            Assert.AreEqual((1000000L, 0), (game.Money, game.compositions.Count));
            Set(vm, "Quantity", 16);
            Assert.IsTrue(command.CanExecute(null));
            StringAssert.Contains(Get<string>(vm, "Description"), "編成価格");
        }

        [TestMethod]
        public void CompositionOwnershipChangeIsSharedByMessageCommandAndConfirmationCallback()
        {
            var game = Game();
            var car = Vehicle();
            game.vehicles.Add(car);
            var vm = Create("CompositionMakeViewModel", game);
            Set(vm, "Name", "入力");
            Set(vm, "Vehicle", car);
            Set(vm, "Quantity", 1);
            var command = Get<ICommand>(vm, "Make");
            Assert.IsTrue(command.CanExecute(null));
            game.vehicles.Remove(car);
            Assert.AreEqual("ゲームに登録されていない車両は使用できません", Get<string>(vm, "ErrorMsg"));
            Assert.IsFalse(command.CanExecute(null));
            command.Execute(null);
            var error = Assert.ThrowsException<TargetInvocationException>(() =>
                command.GetType().GetMethod("Make", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(command, null));
            Assert.AreEqual(CompositionCreationReason.UnregisteredVehicle,
                ((CompositionCreationRejectedException)error.InnerException!).Check.Reason);
            Assert.AreEqual(0, game.compositions.Count);
        }

        [TestMethod]
        public void UnselectedInvalidCarCanBeDescribedWithoutPreventingValidComposition()
        {
            var game = Game();
            var valid = Vehicle();
            var unused = new Car { Name = "未使用", bestSpeed = -1, power = (PowerEnum)999 };
            game.vehicles.AddRange(new[] { valid, unused });
            var vm = Create("CompositionMakeViewModel", game);
            Set(vm, "Name", "入力");
            Set(vm, "Vehicle", valid);
            Set(vm, "Quantity", 1);
            Set(vm, "Vehicle", unused);
            StringAssert.Contains(Get<string>(vm, "Description"), "999（未定義）");
            Assert.IsTrue(Get<ICommand>(vm, "Make").CanExecute(null));
            Set(vm, "Quantity", 1);
            Assert.AreEqual("負の速度の車両は使用できません", Get<string>(vm, "ErrorMsg"));
            Assert.IsFalse(Get<ICommand>(vm, "Make").CanExecute(null));
        }

        private static Car Vehicle() => new() { Name = "車両", bestSpeed = 40, power = PowerEnum.Steam,
            gauge = CarGaugeEnum.Narrow, type = RailTypeEnum.Iron, seat = SeatEnum.Semi };
        private static object Create(string name, GameInfo game)
            => Activator.CreateInstance(typeof(ViewModelBase).Assembly.GetType($"whr_wpf.ViewModel.Vehicle.{name}")!, game, null)!;
        private static void Set(object vm, string name, object value) => vm.GetType().GetProperty(name)!.SetValue(vm, value);
        private static T Get<T>(object vm, string name) => (T)vm.GetType().GetProperty(name)!.GetValue(vm)!;
    }
}
