using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Threading;
using whr_wpf.View.Line;
using whr_wpf.ViewModel;
using whr_wpf.ViewModel.Component;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("UiRegression")]
    public class SelectionBindingTests
    {
        [TestMethod]
        public void ConstructKeepsNormalAndSuspendedInitialSelections()
        {
            OnStaThread(() =>
            {
                foreach (var (suspended, lanes) in new[] { (false, 2), (true, 1), (true, 2), (true, 4) })
                {
                    var game = Game();
                    var line = Line(game, false);
                    typeof(Line).GetProperty(nameof(Line.IsSuspended))!.SetValue(line, suspended);
                    line.LaneNum = lanes;
                    var window = new ConstructWindow(line, game);
                    try
                    {
                        window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                        var vm = (ConstructWindowViewModel)window.DataContext;
                        foreach (string name in new[] { "LaneSu", "RailType", "Taihi" })
                        {
                            var combo = (ComboBox)window.FindName(name);
                            Assert.AreEqual("", combo.SelectedValuePath);
                            Assert.IsNotNull(combo.SelectedItem);
                        }
                        Assert.IsNotNull(vm.LaneSu);
                        Assert.IsNotNull(vm.RailType);
                        Assert.IsNotNull(vm.Taihisen);
                        Assert.AreEqual(suspended ? lanes : 2, ((ConstructWindowViewModel.LaneNumViewModel)((ComboBox)window.FindName("LaneSu")).SelectedItem).LaneSu);
                        var selectedRail = (ConstructWindowViewModel.RailTypeViewModel)((ComboBox)window.FindName("RailType")).SelectedItem;
                        Assert.AreEqual((vm.RailType.RailType, vm.RailType.RailGauge, vm.RailType.IsElectrified),
                            (selectedRail.RailType, selectedRail.RailGauge, selectedRail.IsElectrified));
                        Assert.AreEqual(vm.Taihisen.Enum, ((TaihiViewComponent)((ComboBox)window.FindName("Taihi")).SelectedItem).Enum);
                    }
                    finally { window.Close(); }
                }
            });
        }

        [TestMethod]
        public void ConstructSelectsByValuesAndWritesDifferentValuesBackDespiteEqualCaptions()
        {
            OnStaThread(() =>
            {
                var game = Game();
                var window = new ConstructWindow(Line(game, false), game);
                try
                {
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    var vm = (ConstructWindowViewModel)window.DataContext;
                    var lane = (ComboBox)window.FindName("LaneSu");
                    var rail = (ComboBox)window.FindName("RailType");
                    var taihi = (ComboBox)window.FindName("Taihi");
                    var oldLane = (ConstructWindowViewModel.LaneNumViewModel)lane.SelectedItem;
                    var oldRail = (ConstructWindowViewModel.RailTypeViewModel)rail.SelectedItem;
                    var oldTaihi = (TaihiViewComponent)taihi.SelectedItem;
                    var targetLane = lane.Items.Cast<ConstructWindowViewModel.LaneNumViewModel>().Single(item => item.LaneSu == 1);
                    var targetRail = rail.Items.Cast<ConstructWindowViewModel.RailTypeViewModel>().Single(item => item.RailType == RailTypeEnum.Iron && item.RailGauge == RailGaugeEnum.Regular && !item.IsElectrified);
                    var targetTaihi = taihi.Items.Cast<TaihiViewComponent>().Single(item => item.Enum == TaihisenEnum.Every2km);

                    // ItemsSource内の値と同じ別個体を、異なる表示名でVMから渡す。
                    vm.LaneSu = new() { Caption = "別の線数表示", LaneSu = targetLane.LaneSu };
                    vm.RailType = new() { Caption = "別の軌道表示", RailType = targetRail.RailType, RailGauge = targetRail.RailGauge, IsElectrified = targetRail.IsElectrified };
                    vm.Taihisen = new() { Caption = "別の待避線表示", Enum = targetTaihi.Enum };
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    // 同値の別個体が選択値として残っても、対応する項目と値を選べていればよい。
                    Assert.AreEqual(lane.Items.IndexOf(targetLane), lane.SelectedIndex);
                    Assert.AreEqual(rail.Items.IndexOf(targetRail), rail.SelectedIndex);
                    Assert.AreEqual(taihi.Items.IndexOf(targetTaihi), taihi.SelectedIndex);
                    Assert.AreEqual(targetLane.LaneSu, ((ConstructWindowViewModel.LaneNumViewModel)lane.SelectedItem).LaneSu);
                    var selectedRail = (ConstructWindowViewModel.RailTypeViewModel)rail.SelectedItem;
                    Assert.AreEqual((targetRail.RailType, targetRail.RailGauge, targetRail.IsElectrified),
                        (selectedRail.RailType, selectedRail.RailGauge, selectedRail.IsElectrified));
                    Assert.AreEqual(targetTaihi.Enum, ((TaihiViewComponent)taihi.SelectedItem).Enum);

                    // 同じ表示名を持つ別値をUIで選んでも、その別値がVMへ書き戻る。
                    oldLane.Caption = targetLane.Caption;
                    oldRail.Caption = targetRail.Caption;
                    oldTaihi.Caption = targetTaihi.Caption;
                    lane.SelectedItem = oldLane;
                    rail.SelectedItem = oldRail;
                    taihi.SelectedItem = oldTaihi;
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    Assert.AreSame(oldLane, vm.LaneSu);
                    Assert.AreSame(oldRail, vm.RailType);
                    Assert.AreSame(oldTaihi, vm.Taihisen);
                    Assert.AreNotEqual(targetLane.LaneSu, vm.LaneSu.LaneSu);
                    Assert.AreNotEqual((targetRail.RailType, targetRail.RailGauge, targetRail.IsElectrified), (vm.RailType.RailType, vm.RailType.RailGauge, vm.RailType.IsElectrified));
                    Assert.AreNotEqual(targetTaihi.Enum, vm.Taihisen.Enum);
                }
                finally { window.Close(); }
            });
        }

        [TestMethod]
        public void TaihiSelectedItemUsesEnumAndWritesDifferentEnumBackDespiteEqualCaptions()
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
                    var selected = (TaihiViewComponent)combo.SelectedItem;
                    Assert.AreEqual(line.taihisen, selected.Enum);
                    var vm = window.DataContext;
                    var property = vm.GetType().GetProperty("Taihisen")!;
                    var target = combo.Items.Cast<TaihiViewComponent>().Single(item => item.Enum == TaihisenEnum.Every2km);
                    var clone = new TaihiViewComponent { Enum = target.Enum, Caption = "別の表示名" };
                    property.SetValue(vm, clone);
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    // 以前とは異なるenumを選ぶことで、旧選択の保持を成功と誤認しない。
                    Assert.AreEqual(target.Enum, ((TaihiViewComponent)combo.SelectedItem).Enum);
                    Assert.AreEqual(target.Enum, ((TaihiViewComponent)property.GetValue(vm)!).Enum);
                    selected.Caption = target.Caption;
                    combo.SelectedItem = selected;
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    Assert.AreSame(selected, property.GetValue(vm));
                    Assert.AreNotEqual(target.Enum, ((TaihiViewComponent)property.GetValue(vm)!).Enum);
                }
                finally { window.Close(); }
            });
        }

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
