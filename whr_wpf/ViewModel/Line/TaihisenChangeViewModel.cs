using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.View;
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

		public List<TaihiViewComponent> TaihiList { get; } = TaihiViewComponent.CreateViewList();

		private TaihiViewComponent taihisen;
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
				catch (MoneyShortException e)
				{
					MessageBox.Show(GameFailureFormatter.Format(e.Failure));
				}
				taihisenChangeWindow.Close();
			}
		}

		public TaihisenChangeViewModel(Line line, GameInfo gameInfo, TaihisenChangeWindow taihisenChangeWindow)
		{
			this.line = line;
			this.gameInfo = gameInfo;
			this.taihisenChangeWindow = taihisenChangeWindow;
			// 選択肢と同じインスタンスで現在の設備を選び、初期バインド時の選択解除を防ぐ。
			taihisen = TaihiList.First(item => item.Enum == line.taihisen);

			Kettei = new KetteiCommand(this);
			Cancel = new CancelCommand(this);
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
		public string EstimatedCost => taihisen == null ? "待避線を選択してください" : LogicUtil.AppendMoneyUnit(CalcCost());

		private long CalcCost()
		{
			if (taihisen == null) { throw new InvalidOperationException("待避線を選択してください"); }
			return line.CalcTaihisenChangeCost(taihisen.Enum, gameInfo);
		}

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
				if (!CanExecute(parameter)) { return; }
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
