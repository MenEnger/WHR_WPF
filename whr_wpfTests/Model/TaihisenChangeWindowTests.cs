using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Threading;
using whr_wpf.View.Line;
using whr_wpf.ViewModel.Component;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("UiRegression")]
    public class TaihisenChangeWindowTests
    {
        [TestMethod]
        public void OpeningWindowKeepsEveryExistingSidingSelectionAndQuote()
        {
            OnStaThread(() =>
            {
                foreach (var spacing in Enum.GetValues<TaihisenEnum>())
                {
                    var game = Game();
                    var line = Line(game);
                    line.taihisen = spacing;
                    var window = new TaihisenChangeWindow(line, game);
                    try
                    {
                        // 実際のXAMLをバインドする。ウィンドウは表示しない。
                        window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                        var combo = (ComboBox)window.FindName("Taihi");
                        var selected = combo.SelectedItem as TaihiViewComponent;
                        Assert.IsNotNull(selected, $"初期間隔: {spacing}");
                        Assert.AreEqual(spacing, selected.Enum);
                        var vm = window.DataContext;
                        var type = vm.GetType();
                        Assert.AreSame(selected, type.GetProperty("Taihisen")!.GetValue(vm));
                        Assert.AreSame(type.GetProperty("TaihiList")!.GetValue(vm), combo.ItemsSource);
                        Assert.AreEqual(FormatMoney(line.CalcTaihisenChangeCost(spacing, game)),
                            type.GetProperty("EstimatedCost")!.GetValue(vm));
                        Assert.IsTrue(((Button)window.FindName("Kettei")).Command.CanExecute(null));
                        Assert.IsNotNull(((Button)window.FindName("Cancel")).Command);
                    }
                    finally { window.Close(); }
                }
            });
        }

        [TestMethod]
        public void ClearingSelectionIsSafeAndReselectingUpdatesQuoteAndEquipment()
        {
            OnStaThread(() =>
            {
                var game = Game();
                var line = Line(game);
                line.Name = "京都―大阪";
                line.taihisen = TaihisenEnum.Every50km;
                var window = new TaihisenChangeWindow(line, game);
                try
                {
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    var combo = (ComboBox)window.FindName("Taihi");
                    var decision = (Button)window.FindName("Kettei");
                    var vm = window.DataContext;
                    var type = vm.GetType();
                    long money = game.Money;
                    combo.SelectedItem = null;
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    Assert.IsNull(type.GetProperty("Taihisen")!.GetValue(vm));
                    Assert.AreEqual("待避線を選択してください", type.GetProperty("EstimatedCost")!.GetValue(vm));
                    Assert.IsFalse(decision.Command.CanExecute(null));
                    decision.Command.Execute(null);
                    Assert.AreEqual((money, TaihisenEnum.Every50km), (game.Money, line.taihisen));
                    combo.SelectedItem = combo.Items.Cast<TaihiViewComponent>().Single(item => item.Enum == TaihisenEnum.Every2km);
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    long cost = line.CalcTaihisenChangeCost(TaihisenEnum.Every2km, game);
                    Assert.AreEqual(FormatMoney(cost), type.GetProperty("EstimatedCost")!.GetValue(vm));
                    Assert.IsTrue(decision.Command.CanExecute(null));
                    // 確認ダイアログを開かず、承認後に呼ばれる変更処理を検証する。
                    type.GetMethod("ChangeTaihi", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
                    Assert.AreEqual((money - cost, -cost, TaihisenEnum.Every2km),
                        (game.Money, line.totalBalance, line.taihisen));
                }
                finally { window.Close(); }
            });
        }

        [TestMethod]
        public void CancelClosesWindowWithoutChangingMoneyOrEquipment()
        {
            OnStaThread(() =>
            {
                var game = Game();
                var line = Line(game);
                var window = new TaihisenChangeWindow(line, game);
                try
                {
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    var combo = (ComboBox)window.FindName("Taihi");
                    combo.SelectedItem = combo.Items.Cast<TaihiViewComponent>().Single(item => item.Enum == TaihisenEnum.Every2km);
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    var before = (game.Money, line.totalBalance, line.taihisen);
                    bool closed = false;
                    window.Closed += (_, _) => closed = true;
                    var cancel = (Button)window.FindName("Cancel");
                    Assert.IsNotNull(cancel.Command);
                    Assert.IsTrue(cancel.Command.CanExecute(null));
                    // 選択変更はプレビューだけで、キャンセルでは設備や支出を確定しない。
                    cancel.Command.Execute(null);
                    Assert.IsTrue(closed);
                    Assert.AreEqual(before, (game.Money, line.totalBalance, line.taihisen));
                }
                finally { window.Close(); }
            });
        }

        private static string FormatMoney(long amount)
            => (string)typeof(GameInfo).Assembly.GetType("whr_wpf.Util.LogicUtil")!
                .GetMethod("AppendMoneyUnit")!.Invoke(null, new object[] { amount })!;

        private static void OnStaThread(Action action)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
