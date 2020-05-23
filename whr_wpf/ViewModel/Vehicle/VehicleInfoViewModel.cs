using System.Collections.Generic;
using System.Text;
using System.Windows;
using whr_wpf.Model;
using whr_wpf.Util;

namespace whr_wpf.ViewModel.Vehicle
{
	/// <summary>
	/// 車両情報VM
	/// </summary>
	class VehicleInfoViewModel : ViewModelBase
	{
		public VehicleInfoViewModel(GameInfo gameInfo, Window window)
		{
			this.gameInfo = gameInfo;
			this.window = window;

			Vehicles = gameInfo.vehicles;
		}

		GameInfo gameInfo;
		private Car vehicle;

		public List<Car> Vehicles { get; set; }
		public Car Vehicle
		{
			get => vehicle; set
			{
				vehicle = value;
				OnPropertyChanged(nameof(Description));
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

				builder.Append($"動力　{Vehicle.power.ToName()}\n");

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

				builder.Append($"車両価格　{LogicUtil.AppendMoneyUnit(Vehicle.money)}\n");

				return builder.ToString();

			}
		}
	}
}
