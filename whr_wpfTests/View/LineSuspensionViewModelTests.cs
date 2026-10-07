using System.Windows;
using whr_wpf.ViewModel;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("SelectedBehavior")]
    public class LineSuspensionViewModelTests
    {
        [TestMethod]
        public void ReconstructionUsesRetainedSettingsAndChargesConstructionCost()
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = 1;
            line.bestSpeed = 80;
            line.gauge = RailGaugeEnum.Regular;
            line.IsElectrified = true;
            line.taihisen = TaihisenEnum.Every50km;
            line.totalBalance = -12345;
            line.ReduceOrRemoveLane(game);
            var vm = new ConstructWindowViewModel(line, game, null!);
            Assert.AreEqual((80, 1, RailGaugeEnum.Regular, true, TaihisenEnum.Every50km),
                (vm.BestSpeed, vm.LaneSu.LaneSu, vm.RailType.RailGauge!.Value, vm.RailType.IsElectrified, vm.Taihisen.Enum));
            long cost = vm.CalcCost();
            Assert.IsTrue(cost > 0);
            game.Money = 0;
            Assert.ThrowsException<MoneyShortException>(() => vm.Construct());
            Assert.AreEqual((false, true, -12345L), (line.IsExist, line.IsSuspended, line.totalBalance));
            game.Money = 1000000;
            vm.Construct();
            Assert.AreEqual((true, false, 1000000L - cost, -12345L - cost),
                (line.IsExist, line.IsSuspended, game.Money, line.totalBalance));
            Assert.AreEqual((0, 0), (line.runningPerDay, line.useCompositionNum));
        }

        [TestMethod]
        public void SuspensionUiShowsRetainedEquipmentAndReconstructionCost()
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = 1;
            line.ReduceOrRemoveLane(game);
            var vm = new LineInfoViewModel(line, null!, game);
            Assert.AreEqual("休止中（設備保持）", vm.LineStatus);
            Assert.AreEqual(Visibility.Visible, vm.EquipmentVisibility);
            Assert.AreEqual(Visibility.Collapsed, vm.IsExist);
            Assert.AreEqual("路線再建", vm.ConstructionLabel);
            StringAssert.Contains(vm.ConstructionDescription, "建設費");
            Assert.IsFalse(vm.Reform.CanExecute(null));
            Assert.IsFalse(vm.LineDiagram.CanExecute(null));
            Assert.IsTrue(vm.Construction.CanExecute(null));
            var through = Through(game, line);
            var diagramVm = new DiagramInfoViewModel(through, null!, game);
            Assert.AreEqual("休止区間あり", diagramVm.DiagramSectionInfo);
            var unbuilt = Line(game, false);
            through.route.Add(unbuilt);
            Assert.AreEqual("休止・未建設区間あり", diagramVm.DiagramSectionInfo);
        }

    }
}
