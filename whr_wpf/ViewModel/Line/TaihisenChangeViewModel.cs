using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View.Line;
using whr_wpf.ViewModel.Component;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// 待避線変更VM
	/// </summary>
	class TaihisenChangeViewModel : ViewModelBase
	{
		public ICommand Kettei { get; set; }
		public ICommand Cancel { get; set; }
		public string EstimateCost { get; set; }

		public static List<TaihiViewComponent> TaihiList => TaihiViewComponent.CreateViewList();

		private TaihiViewComponent taihisen = TaihiList[3];
		private Line line;
		private GameInfo gameInfo;
		private TaihisenChangeWindow taihisenChangeWindow;

		void Execute(string message, ExecuteDelegete exec)
		{
			MessageBoxResult constructConfirm = MessageBox.Show(message, "", MessageBoxButton.YesNo);
			if (constructConfirm == MessageBoxResult.Yes)
			{
				try
				{
					exec();
				}
				catch (MoneyShortException)
				{
					MessageBox.Show("お金が足りません");
				}
				taihisenChangeWindow.Close();
			}
		}

		public TaihisenChangeViewModel(Line line, GameInfo gameInfo, TaihisenChangeWindow taihisenChangeWindow)
		{
			this.line = line;
			this.gameInfo = gameInfo;
			this.taihisenChangeWindow = taihisenChangeWindow;

			Kettei = new KetteiCommand(this);
		}

		public TaihiViewComponent Taihisen
		{
			get => taihisen; set
			{
				taihisen = value;
				this.OnPropertyChanged(nameof(Taihisen));
				this.OnPropertyChanged(nameof(EstimatedCost));
			}
		}
		public string EstimatedCost => LogicUtil.AppendMoneyUnit(CalcCost());

		private long CalcCost() => line.CalcTaihisenChangeCost(taihisen.Enum, gameInfo);

		private void ChangeTaihi()
		{
			line.ChangeTaihi(Taihisen.Enum, gameInfo);
		}

		private void Close()
		{
			taihisenChangeWindow.Close();
		}

		/// <summary>
		/// 決定
		/// </summary>
		public class KetteiCommand : CommandBase
		{
			private TaihisenChangeViewModel vm;

			public KetteiCommand(TaihisenChangeViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => vm.Taihisen != null;

			public override void Execute(object parameter)
			{
				string text = $"待避線を{vm.Taihisen.Caption}に変更すると{vm.CalcCost()}拾万円かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ChangeTaihi);
				vm.Execute(text, exec);
			}
		}

		/// <summary>
		/// キャンセル
		/// </summary>
		public class CancelCommand : CommandBase
		{
			private TaihisenChangeViewModel vm;

			public CancelCommand(TaihisenChangeViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => true;

			public override void Execute(object parameter) => vm.Close();
		}


	}
}
