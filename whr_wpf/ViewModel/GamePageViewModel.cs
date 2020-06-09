using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View.Info;
using whr_wpf.View.Technology;
using whr_wpf.View.Vehicle;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// ゲームページVM
	/// </summary>
	class GamePageViewModel : ViewModelBase, IDisposable
	{
		IDisposable listener;

		private GameInfo GameInfo { get; set; }
		private GamePage page;
		public ICommand KeitoDaiya { get; set; }
		public ICommand NextWeek { get; set; }
		public ICommand NextMonth { get; set; }
		public ICommand Line { get; set; }
		public ICommand TechDevelop { get; set; }
		public ICommand Vehicle { get; set; }
		public ICommand InfoDiag { get; set; }
		public ICommand Save { get; set; }
		public ICommand Exit { get; set; }

		public ICommand LineInfo { get; set; }
		public ICommand Town { get; set; }

		public ObservableCollection<Line> Lines { get; set; }
		public ObservableCollection<Station> Stations { get; set; }

		public HorizontalAlignment InfoPosiX
			=> GameInfo.InfoPosi == InfoPosiEnum.TopLeft || GameInfo.InfoPosi == InfoPosiEnum.BottomLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right;
		public VerticalAlignment InfoPosiY
			=> GameInfo.InfoPosi == InfoPosiEnum.TopLeft || GameInfo.InfoPosi == InfoPosiEnum.TopRight ? VerticalAlignment.Top : VerticalAlignment.Bottom;
		public string Date => $"{GameInfo.Year}年{GameInfo.Month}月{GameInfo.Week}週目";
		public string Money => $"所持金   {LogicUtil.AppendMoneyUnit(GameInfo.Money)}";
		public Brush MoneyColor => GameInfo.Money >= 0 ? Brushes.White : Brushes.Red;
		public string Income => $"前週収入   {LogicUtil.AppendMoneyUnit(GameInfo.income)}";
		public string Outlay => $"前週支出   {LogicUtil.AppendMoneyUnit(GameInfo.outlay)}";
		public string Benefit => $"収支合計   {LogicUtil.AppendMoneyUnit(GameInfo.income - GameInfo.outlay)}";
		public Brush BenefitColor => GameInfo.income - GameInfo.outlay >= 0 ? Brushes.White : Brushes.Red;


		private void PropertyChangedInGameInfo(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(GameInfo.Money):
					OnPropertyChanged(nameof(Money));
					OnPropertyChanged(nameof(MoneyColor));
					break;
				case nameof(GameInfo.Year):
				case nameof(GameInfo.Month):
				case nameof(GameInfo.Week):
					OnPropertyChanged(nameof(Date));
					break;
				case nameof(GameInfo.income):
					OnPropertyChanged(nameof(Income));
					OnPropertyChanged(nameof(Benefit));
					OnPropertyChanged(nameof(BenefitColor));
					break;
				case nameof(GameInfo.outlay):
					OnPropertyChanged(nameof(Outlay));
					OnPropertyChanged(nameof(Benefit));
					OnPropertyChanged(nameof(BenefitColor));
					break;
			}
		}

		bool inProcessing = false;

		public GamePageViewModel(GameInfo gameInfo, GamePage page)
		{
			this.GameInfo = gameInfo;
			this.page = page;

			KeitoDaiya = new KeitoDaiyaCommand(this);
			NextWeek = new NextWeekCommand(this);
			NextMonth = new NextMonthCommand(this);
			Line = new LineCommand(this);
			TechDevelop = new TechDevCommand(this);
			Vehicle = new VehicleCommand(this);
			InfoDiag = new InfoDiagCommand(this);
			Save = new SaveCommand(this);
			Exit = new ExitCommand(this);

			LineInfo = new LineInfoCommand(this);
			Town = new TownCommand(this);

			listener = new PropertyChangedEventListener(this.GameInfo, PropertyChangedInGameInfo);

			Lines = new ObservableCollection<Line>(gameInfo.lines);
			Stations = new ObservableCollection<Station>(gameInfo.stations);
		}

		public void Dispose()
		{
			listener?.Dispose();
		}

		private void DoNextWeek()
		{
			List<string> msgs = GameInfo.NextWeek();
			try
			{
				if (msgs.Count > 0)
				{
					MessageBox.Show(string.Join("\n===\n", msgs));
				}
			}
			catch (GameOverException e)
			{
				MessageBox.Show(e.Message, "ゲームオーバー", MessageBoxButton.OK, MessageBoxImage.Information);
				ApplicationUtil.ForceExit();
			} 
		}

		private void DoNextMonth()
		{
			List<string> msgs = new List<string>();
			try
			{
				for (int i = 0; i < 4; i++)
				{
					msgs.AddRange(GameInfo.NextWeek());
					InvokeAllNotify();
				}
				if (msgs.Count > 0)
				{
					MessageBox.Show(string.Join("\n===\n", msgs));
				}
			}
			catch (GameOverException e)
			{
				MessageBox.Show(e.Message, "ゲームオーバー", MessageBoxButton.OK, MessageBoxImage.Information);
				ApplicationUtil.ForceExit();
			}
		}

		//上部メニューで使うコマンド

		class KeitoDaiyaCommand : CommandBase
		{
			GamePageViewModel vm;
			public KeitoDaiyaCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter)
			{
				var page = new DiagramInfoPage(vm.GameInfo.lastSeenKeito ?? vm.GameInfo.diagrams[0], vm.GameInfo);
				var window = new ToolWindow(page);
				window.ShowDialog();
			}
		}

		class NextWeekCommand : CommandBase
		{
			GamePageViewModel vm;
			public NextWeekCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;

			public override void Execute(object parameter)
			{
				vm.inProcessing = true;
				vm.InvokeAllNotify();
				vm.DoNextWeek();
				vm.inProcessing = false;
				vm.InvokeAllNotify();
			}
		}

		class NextMonthCommand : CommandBase
		{
			GamePageViewModel vm;
			public NextMonthCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;

			public override void Execute(object parameter)
			{
				vm.inProcessing = true;
				vm.InvokeAllNotify();
				vm.DoNextMonth();
				vm.inProcessing = false;
				vm.InvokeAllNotify();
			}
		}

		class LineCommand : CommandBase
		{
			GamePageViewModel viewModel;
			public LineCommand(GamePageViewModel vm) => this.viewModel = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter)
			{
				var page = new LineInfoPage(viewModel.GameInfo.lastSeenLine ?? viewModel.GameInfo.lines[0], viewModel.GameInfo);
				var window = new ToolWindow(page);
				window.ShowDialog();
			}
		}

		class TechDevCommand : CommandBase
		{
			GamePageViewModel vm;
			public TechDevCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;
			public override void Execute(object parameter)
			{
				var window = new TechnologyDevelopWindow(vm.GameInfo);
				window.Show();
			}
		}

		class VehicleCommand : CommandBase
		{
			GamePageViewModel vm;
			public VehicleCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;
			public override void Execute(object parameter) => new VehicleActionListWindow(vm.GameInfo).ShowDialog();
		}

		class InfoDiagCommand : CommandBase
		{
			GamePageViewModel vm;
			public InfoDiagCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;
			public override void Execute(object parameter) => new InfoWindow(vm.GameInfo).ShowDialog();
		}

		class SaveCommand : CommandBase
		{
			GamePageViewModel vm;
			public SaveCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;
			public override void Execute(object parameter) => ApplicationUtil.SaveData(vm.GameInfo);
		}

		class ExitCommand : CommandBase
		{
			GamePageViewModel vm;
			public ExitCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;
			public override void Execute(object parameter) => ApplicationUtil.Exit(vm.GameInfo);
		}

		//canvas内要素で使うコマンド

		class LineInfoCommand : CommandBase
		{
			GamePageViewModel vm;
			public LineInfoCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;
			public override void Execute(object parameter)
			{
				var page = new LineInfoPage((Line)parameter, vm.GameInfo);
				var window = new ToolWindow(page);

				window.ShowDialog();
			}
		}

		class TownCommand : CommandBase
		{
			GamePageViewModel vm;
			public TownCommand(GamePageViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => !vm.inProcessing;
			public override void Execute(object parameter)
			{
				var vm = new TownViewModel((Station)parameter);
				var window = new TownWindow(vm, this.vm.GameInfo);
				window.ShowDialog();
			}
		}
	}
}
