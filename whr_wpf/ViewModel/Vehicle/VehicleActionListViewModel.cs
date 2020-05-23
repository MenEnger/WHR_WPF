using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.View.Vehicle;

namespace whr_wpf.ViewModel.Vehicle
{
	/// <summary>
	/// 車両メニューVM
	/// </summary>
	class VehicleActionListViewModel : ViewModelBase
	{
		public VehicleActionListViewModel(GameInfo gameInfo, Window window)
		{
			this.gameInfo = gameInfo;
			this.window = window;

			this.CompositionManage = new CompositionManageCommand(this);
			this.CompositionMake = new CompositionMakeCommand(this);
			this.VehicleInfo = new VehicleInfoCommand(this);
			this.VehicleDevelop = new VehicleDevelopCommand(this);
		}


		GameInfo gameInfo;

		public ICommand CompositionManage { get; set; }
		public ICommand CompositionMake { get; set; }
		public ICommand VehicleInfo { get; set; }
		public ICommand VehicleDevelop { get; set; }

		public class CompositionManageCommand : CommandBase
		{
			private VehicleActionListViewModel vm;
			public CompositionManageCommand(VehicleActionListViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => new CompositionManageWindow(vm.gameInfo).ShowDialog();
		}

		public class CompositionMakeCommand : CommandBase
		{
			private VehicleActionListViewModel vm;
			public CompositionMakeCommand(VehicleActionListViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => new CompositionMakeWindow(vm.gameInfo).ShowDialog();
		}

		public class VehicleInfoCommand : CommandBase
		{
			private VehicleActionListViewModel vm;
			public VehicleInfoCommand(VehicleActionListViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => new VehicleInfoWindow(vm.gameInfo).ShowDialog();
		}

		public class VehicleDevelopCommand : CommandBase
		{
			private VehicleActionListViewModel vm;
			public VehicleDevelopCommand(VehicleActionListViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => new VehicleDevelopWindow(vm.gameInfo).ShowDialog();
		}
	}
}
