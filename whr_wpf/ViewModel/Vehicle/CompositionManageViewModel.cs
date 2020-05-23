using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;

namespace whr_wpf.ViewModel.Vehicle
{
	/// <summary>
	/// 編成情報/売買VM
	/// </summary>
	class CompositionManageViewModel : ViewModelBase
	{
		public CompositionManageViewModel(GameInfo gameInfo, Window window)
		{
			this.gameInfo = gameInfo;
			this.window = window;

			Compositions = gameInfo.compositions;

			QuantUp = new UpCommand(this);
			QuantDown = new DownCommand(this);
			Buy = new BuyCommand(this);
			Sale = new SaleCommand(this);
		}

		GameInfo gameInfo;
		private IComposition composition;
		private int quantity;

		public List<IComposition> Compositions { get; set; }
		public IComposition Composition
		{
			get => composition; set
			{
				composition = value;
				OnPropertyChanged(nameof(Description));
				OnPropertyChanged(nameof(PriceInfo));
			}
		}

		public string Description
		{
			get
			{
				if (Composition is null) { return ""; }

				StringBuilder builder = new StringBuilder();
				builder.Append($"最高速度　{Composition.BestSpeed}km/h\n");

				if (Composition.Gauge == CarGaugeEnum.Narrow || Composition.Gauge == CarGaugeEnum.Regular)
				{
					builder.Append($"軌道　{Composition.Gauge.ToName()}\n");
				}
				else if (Composition.Gauge == CarGaugeEnum.FreeGauge)
				{
					builder.Append("フリーゲージトレイン\n");
				}

				builder.Append($"動力　{Composition.Power.ToName()}\n");

				switch (Composition.Tilt)
				{
					case CarTiltEnum.Pendulum:
					case CarTiltEnum.SimpleMecha:
						builder.Append("車体傾斜装置\n");
						break;
					case CarTiltEnum.HighMecha:
						builder.Append("高性能車体傾斜装置\n");
						break;
				}

				builder.Append($"編成両数　{Composition.CarCount}両\n");
				builder.Append($"余剰編成数　{Composition.HeldUnits}編成\n");
				builder.Append($"編成価格　{LogicUtil.AppendMoneyUnit(Composition.Price)}\n");

				return builder.ToString();

			}
		}

		public int Quantity
		{
			get => quantity; set
			{
				quantity = value;
				OnPropertyChanged(nameof(Quantity));
				OnPropertyChanged(nameof(PriceInfo));
			}
		}

		public string PriceInfo
		{
			get
			{
				if (Composition is null) { return ""; }

				StringBuilder builder = new StringBuilder();
				builder.Append($"合計購入価格　{LogicUtil.AppendMoneyUnit(CalcBuyPrice())}\n");
				builder.Append($"合計売却価格　{LogicUtil.AppendMoneyUnit(CalcSalePrice())}\n");
				return builder.ToString();
			}
		}

		private int CalcBuyPrice() => Composition.Price * Quantity;
		private int CalcSalePrice() => Composition.SalePrice * Quantity;

		public ICommand QuantUp { get; set; }
		public ICommand QuantDown { get; set; }
		public ICommand Buy { get; set; }
		public ICommand Sale { get; set; }


		/// <summary>
		/// 数量増加
		/// </summary>
		public class UpCommand : CommandBase
		{
			private CompositionManageViewModel vm;
			private const int increments = 1;

			public UpCommand(CompositionManageViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.Quantity += increments;
		}

		/// <summary>
		/// 数量減少
		/// </summary>
		public class DownCommand : CommandBase
		{
			private CompositionManageViewModel vm;
			private const int diff = 1;

			public DownCommand(CompositionManageViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => (vm.Quantity - diff) >= 0;
			public override void Execute(object parameter) => vm.Quantity -= diff;
		}

		/// <summary>
		/// 購入
		/// </summary>
		public class BuyCommand : CommandBase
		{
			private CompositionManageViewModel vm;
			public BuyCommand(CompositionManageViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter) => vm.Quantity > 0;

			public override void Execute(object parameter)
			{
				string text = $"編成を開発するには{LogicUtil.AppendMoneyUnit(vm.CalcBuyPrice())}拾万円かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(Buy);
				vm.ExecuteWithMoney(text, exec);
			}

			private void Buy() => vm.Composition.Purchase(vm.gameInfo, vm.Quantity);
		}

		/// <summary>
		/// 売却
		/// </summary>
		public class SaleCommand : CommandBase
		{
			private CompositionManageViewModel vm;
			public SaleCommand(CompositionManageViewModel viewModel) => vm = viewModel;
			public override bool CanExecute(object parameter)
			{
				return vm.Composition is null ? false
					: vm.Quantity == 0 ? false
					: (vm.Composition.HeldUnits - vm.Quantity) >= 0;
			}

			public override void Execute(object parameter)
			{
				string text = $"編成を売却します。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(Sale);
				vm.ExecuteWithMoney(text, exec);
			}

			private void Sale() => vm.Composition.Sale(vm.gameInfo, vm.Quantity);
		}
	}
}
