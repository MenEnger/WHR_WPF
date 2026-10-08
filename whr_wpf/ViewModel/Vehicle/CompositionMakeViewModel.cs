using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View;

namespace whr_wpf.ViewModel.Vehicle
{
	/// <summary>
	/// 編成作成ウィンドウVM
	/// </summary>
	class CompositionMakeViewModel : ViewModelBase
	{
		public CompositionMakeViewModel(GameInfo gameInfo, Window window)
		{
			this.gameInfo = gameInfo;
			this.window = window;

			VehicleNums = gameInfo.vehicles.ToDictionary(vehicle => vehicle, _ => 0);

			QuantUp = new UpCommand(this);
			QuantDown = new DownCommand(this);
			Make = new MakeCommand(this);
		}

		GameInfo gameInfo;
		private Car vehicle;
		private string name;

		/// <summary>
		/// 構成車両-構成両数の組
		/// </summary>
		public Dictionary<Car, int> VehicleNums { get; set; }

		public string Name
		{
			get => name; set
			{
				name = value;
				OnPropertyChanged(nameof(ErrorMsg));
				OnPropertyChanged(nameof(Make));
			}
		}

		public Car Vehicle
		{
			get => vehicle; set
			{
				vehicle = value;
				OnPropertyChanged(nameof(Description));
				OnPropertyChanged(nameof(Quantity));
			}
		}

		public string Description
		{
			get
			{
				if (Vehicle is null) { return ""; }

				StringBuilder builder = new StringBuilder();
				builder.Append($"最高速度　{Vehicle.bestSpeed}km/h\n");

				if (Vehicle.gauge == CarGaugeEnum.Narrow || Vehicle.gauge == CarGaugeEnum.Regular)
				{
					builder.Append($"軌道　{Vehicle.gauge.ToName()}\n");
				}
				else if (Vehicle.gauge == CarGaugeEnum.FreeGauge)
				{
					builder.Append("フリーゲージトレイン\n");
				}

				string powerName = Enum.IsDefined(Vehicle.power) ? Vehicle.power.ToName() : $"{(int)Vehicle.power}（未定義）";
				builder.Append($"動力　{powerName}\n");

				switch (Vehicle.carTilt)
				{
					case CarTiltEnum.Pendulum:
					case CarTiltEnum.SimpleMecha:
						builder.Append("車体傾斜装置\n");
						break;
					case CarTiltEnum.HighMecha:
						builder.Append("高性能車体傾斜装置\n");
						break;
				}

				builder.Append($"車両価格　{MoneyDisplayFormatter.Format(Vehicle.money)}\n");

				builder.Append("\n");

				// 名前入力前にも説明を表示する。実際の作成は実名で別途検査する。
				var check = gameInfo.CheckCreateComposition("見積", VehicleNums);
				if (!check.CanCompositionMake && check.Reason != CompositionCreationReason.NoVehicles)
				{
					builder.Append(CreationValidationFormatter.Format(check));
					return builder.ToString();
				}
				builder.Append($"最高速度　{CompositionFactory.CalcCompositionBestSpeed(VehicleNums)}km/h\n");

				int price = VehicleNums.Sum(vehicle => vehicle.Key.money * vehicle.Value);
				builder.Append($"編成価格　{MoneyDisplayFormatter.Format(price)}\n");

				return builder.ToString();
			}
		}

		public string ErrorMsg
		{
			get
			{
				if (Vehicle is null) { return ""; }

				var check = gameInfo.CheckCreateComposition(Name, VehicleNums);

				if (check.CanCompositionMake) { return ""; }

				return CreationValidationFormatter.Format(check);
			}
		}

		public int Quantity
		{
			get => Vehicle is null ? 0 : VehicleNums[vehicle];

			set
			{
				if (Vehicle is null) { return; }
				VehicleNums[Vehicle] = value;
				OnPropertyChanged(nameof(Quantity));
				OnPropertyChanged(nameof(Description));
				OnPropertyChanged(nameof(ErrorMsg));
				OnPropertyChanged(nameof(Make));
			}
		}

		private bool CanCreateComposition()
		{
			return gameInfo.CheckCreateComposition(Name, VehicleNums).CanCompositionMake;
		}

		public ICommand QuantUp { get; set; }
		public ICommand QuantDown { get; set; }
		public ICommand Make { get; set; }

		/// <summary>
		/// 数量増加
		/// </summary>
		public class UpCommand : CommandBase
		{
			private CompositionMakeViewModel vm;
			private const int increments = 1;

			public UpCommand(CompositionMakeViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => vm.Quantity < CompositionFactory.MaximumVehiclesPerType;
			public override void Execute(object parameter) => vm.Quantity += increments;
		}

		/// <summary>
		/// 数量減少
		/// </summary>
		public class DownCommand : CommandBase
		{
			private CompositionMakeViewModel vm;
			private const int diff = 1;

			public DownCommand(CompositionMakeViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => vm.Quantity > 0;
			public override void Execute(object parameter) => vm.Quantity -= diff;
		}

		/// <summary>
		/// 編成作成
		/// </summary>
		public class MakeCommand : CommandBase
		{
			private CompositionMakeViewModel vm;
			public MakeCommand(CompositionMakeViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => vm.CanCreateComposition();

			public override void Execute(object parameter)
			{
				if (!vm.CanCreateComposition())
				{
					vm.OnPropertyChanged(nameof(ErrorMsg));
					return;
				}
				string text = $"編成を作成してよろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(Make);
				vm.ExecuteWithMoney(text, exec);
			}

			void Make()
			{
				vm.gameInfo.CreateComposition(vm.Name, vm.VehicleNums);
			}

		}
	}
}
