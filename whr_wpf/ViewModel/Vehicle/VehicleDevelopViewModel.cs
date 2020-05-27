using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;

namespace whr_wpf.ViewModel.Vehicle
{
	/// <summary>
	/// 車両開発VM
	/// </summary>
	class VehicleDevelopViewModel : ViewModelBase
	{
		public VehicleDevelopViewModel(GameInfo gameInfo, Window window)
		{
			this.gameInfo = gameInfo;
			this.window = window;

			this.SpeedUp = new SpeedUpCommand(this);
			this.SpeedDown = new SpeedDownCommand(this);
			this.Kettei = new KetteiCommand(this);

			Name = $"{gameInfo.vehicles.Count + 1 + 100}系";
			BestSpeed = 120;
			Seat = SeatEnum.None;

			TiltList = typeof(CarTiltEnum)
				.GetEnumValues()
				.Cast<CarTiltEnum>()
				.Where(tilt =>
				{
					switch (tilt)
					{
						case CarTiltEnum.None:
							return true;
						case CarTiltEnum.Pendulum:
							return gameInfo.CanUsePendulum();
						case CarTiltEnum.SimpleMecha:
						case CarTiltEnum.HighMecha:
							return gameInfo.isDevelopedMachineTilt;
						default:
							throw new NotImplementedException($"車体傾斜装置{tilt}の表示条件が未定義です");
					}
				})
				.ToImmutableDictionary(seatEnum => seatEnum, seatEnum => seatEnum.ToName());

		}

		private GameInfo gameInfo;
		private SeatEnum seat;
		private int bestSpeed;
		private CarGaugeEnum gauge;
		private PowerEnum power;
		private bool isDoubleDecker;
		private CarTiltEnum carTilt;


		/// <summary>
		/// 動力リスト
		/// </summary>
		public static ImmutableDictionary<PowerEnum, string> PowerList =>
			typeof(PowerEnum)
			.GetEnumValues()
			.Cast<PowerEnum>()
			.ToImmutableDictionary(powerEnum => powerEnum, powerEnum => powerEnum.ToName());

		/// <summary>
		/// 軌間リスト
		/// </summary>
		public static ImmutableDictionary<CarGaugeEnum, string> GaugeList =>
			typeof(CarGaugeEnum)
			.GetEnumValues()
			.Cast<CarGaugeEnum>()
			.ToImmutableDictionary(gaugeEnum => gaugeEnum, gaugeEnum => gaugeEnum.ToName());

		/// <summary>
		/// 座席リスト
		/// </summary>
		public static ImmutableDictionary<SeatEnum, string> SeatList =>
			typeof(SeatEnum)
			.GetEnumValues()
			.Cast<SeatEnum>()
			.Where(seatEnum => seatEnum != SeatEnum.DoubleDeckerRich && seatEnum != SeatEnum.DoubleDeckerRotatable)
			.ToImmutableDictionary(seatEnum => seatEnum, seatEnum => seatEnum.ToName());

		/// <summary>
		/// 車体傾斜装置リスト
		/// </summary>
		public ImmutableDictionary<CarTiltEnum, string> TiltList { get; set; }

		public string Name { get; set; }
		public int BestSpeed
		{
			get => bestSpeed; set
			{
				bestSpeed = value;
				InvokeAllNotify();
			}
		}
		public PowerEnum Power
		{
			get => power; set
			{
				power = value;
				InvokeAllNotify();
			}
		}
		public CarGaugeEnum Gauge
		{
			get => gauge; set
			{
				gauge = value;
				InvokeAllNotify();
			}
		}
		public SeatEnum Seat
		{
			get => seat; set
			{
				seat = value;
				InvokeAllNotify();
			}
		}
		public bool IsDoubleDecker
		{
			get => isDoubleDecker; set
			{
				isDoubleDecker = value;
				InvokeAllNotify();
			}
		}
		public bool CanDoubleDecker => Seat == SeatEnum.Rotatable || Seat == SeatEnum.Rich;

		public CarTiltEnum CarTilt
		{
			get => carTilt; set
			{
				carTilt = value;
				OnPropertyChanged(nameof(Msg));
				OnPropertyChanged(nameof(Kettei));
			}
		}

		/// <summary>
		/// 二階建てチェックを考慮した座席タイプ取得
		/// </summary>
		/// <returns></returns>
		private SeatEnum GetSeatWithDoubleDecker()
		{
			return Seat switch
			{
				SeatEnum.Rich when IsDoubleDecker => SeatEnum.DoubleDeckerRich,
				SeatEnum.Rotatable when IsDoubleDecker => SeatEnum.DoubleDeckerRotatable,
				_ => Seat
			};
		}

		public string Msg
		{
			get
			{
				(bool canCreate, string msg) = CheckCreateVehicle();
				if (!canCreate) { return msg; }

				return $"車両価格　{LogicUtil.AppendMoneyUnit(CalcVehiclePrice())}";
			}
		}

		public ICommand SpeedUp { get; set; }
		public ICommand SpeedDown { get; set; }
		public ICommand Kettei { get; set; }

		/// <summary>
		/// 車両を開発できるかチェック
		/// </summary>
		/// <returns></returns>
		private (bool CanCreateVehicle, string msg) CheckCreateVehicle() =>
			gameInfo.CheckCreateVehicle(Name, BestSpeed, Power, Gauge, GetSeatWithDoubleDecker(), CarTilt);

		/// <summary>
		/// 車両価格計算
		/// </summary>
		/// <returns></returns>
		private int CalcVehiclePrice() => gameInfo.CalcPurchaseVehicleCost(bestSpeed, power, gauge, GetSeatWithDoubleDecker(), CarTilt);

		/// <summary>
		/// 車両開発費計算
		/// </summary>
		/// <returns></returns>
		private int CalcDevelopVehiclePrice() => gameInfo.CalcDevelopVehicleCost(bestSpeed, power, gauge, GetSeatWithDoubleDecker(), CarTilt);

		/// <summary>
		/// 車両を開発
		/// </summary>
		private void ExecuteDevelop() => gameInfo.DevelopVehicle(Name, BestSpeed, Power, Gauge, GetSeatWithDoubleDecker(), CarTilt);

		/// <summary>
		/// 速度増加
		/// </summary>
		public class SpeedUpCommand : CommandBase
		{
			private VehicleDevelopViewModel vm;
			private int increments = 10;

			public SpeedUpCommand(VehicleDevelopViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => (vm.BestSpeed + increments) <= 990;
			public override void Execute(object parameter) => vm.BestSpeed += increments;
		}

		/// <summary>
		/// 速度減少
		/// </summary>
		public class SpeedDownCommand : CommandBase
		{
			private VehicleDevelopViewModel vm;
			private int diff = 10;

			public SpeedDownCommand(VehicleDevelopViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => (vm.BestSpeed - diff) >= 0;
			public override void Execute(object parameter) => vm.BestSpeed -= diff;
		}

		/// <summary>
		/// 決定
		/// </summary>
		public class KetteiCommand : CommandBase
		{
			private VehicleDevelopViewModel vm;
			public KetteiCommand(VehicleDevelopViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter)
			{
				(bool res, _) = vm.CheckCreateVehicle();
				return res;
			}

			public override void Execute(object parameter)
			{
				string text = $"この車両を開発するには{LogicUtil.AppendMoneyUnit(vm.CalcDevelopVehiclePrice())}拾万円かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteDevelop);
				vm.ExecuteWithMoney(text, exec);
			}
		}

	}
}
