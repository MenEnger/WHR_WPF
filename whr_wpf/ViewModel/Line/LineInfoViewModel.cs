using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View.Line;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// 路線情報ダイアログのVM
	/// </summary>
	public class LineInfoViewModel : ViewModelBase
	{
		public Line line;
		private LineInfoPage page;
		private GameInfo gameInfo;

		public Visibility IsExist
		{
			get
			{
				var conv = new BooleanToVisibilityConverter();
				return (Visibility)conv.Convert(line.IsExist, null, null, null);
			}
		}
		public string LineName => line.Name;
		public string Section => $"区間　{line.Start.Name}～{line.End.Name}";
		public string GradeType => $"{line.grade.ToName()}：{line.propertyType.ToName()}";
		public string Distance => $"距離　約{line.Distance}km";
		public string RailInfo
		{
			get
			{
				string text = $"レール幅　";
				if (line.Type == RailTypeEnum.Iron)
				{
					if (line.gauge == RailGaugeEnum.Narrow) text += "狭軌";
					else if (line.gauge == RailGaugeEnum.Regular) text += "標準軌";

					if (line.IsElectrified.HasValue && (bool)line.IsElectrified) text += "（電化済）";
				}
				else if (line.Type == RailTypeEnum.LinearMotor) text += "リニアレール";
				return text;
			}
		}
		public string BestSpeed => $"路線限界速度　{line.bestSpeed}km/h";
		public string RailNum
		{
			get
			{
				string text = "路線数　";
				if (line.LaneNum == 1) text += "単線";
				else if (line.LaneNum == 2) text += "複線";
				else if (line.LaneNum == 4) text += "複々線";
				else if (line.LaneNum % 2 == 1) text += $"{line.LaneNum}線";
				else if (line.LaneNum % 2 == 0) text += $"{line.LaneNum / 2}複線";
				return text;
			}
		}
		public string Taihi => $"待避線　{line.taihisen.ToName()}";
		public string Diagram => line.diagram.ToName();
		public string Ryokaku => $"旅客列車　{line.TotalNumberTrips(false)}本/日";
		public string Kamotu => $"貨物運行数　{line.kamotsuNumLastWeek}本/日";
		public string Genkai => $"限界本数　{line.GenkaiHonsuuUnderCurrent(gameInfo.genkaikyoyo)}本/日";
		public string Joshasuu => $"乗車数　{((line.passengersLastWeek == 0) ? 0 : line.passengersLastWeek * 100)}人";
		public string Josharitsu => $"乗車率　{line.Josharitsu}%";
		public string Shushi => $"収支　{LogicUtil.AppendMoneyUnit(line.incomeLastWeek - line.outlayLastWeek)}";
		public string ShushiColor => (line.incomeLastWeek - line.outlayLastWeek) >= 0 ? "#FFFFFF" : "#FF0000";
		public string TotalShushi => $"総合収支　 {LogicUtil.AppendMoneyUnit(line.totalBalance)}";
		public string TotalShushiColor => line.totalBalance >= 0 ? "#FFFFFF" : "#FF0000";

		public ICommand Close { get; set; }
		public ICommand Construction { get; set; }
		public ICommand Reform { get; set; }
		public ICommand LineDiagram { get; set; }

		public LineInfoViewModel(Line line, LineInfoPage lineInfoPage, GameInfo gameInfo)
		{
			this.line = line;
			this.page = lineInfoPage;
			this.Close = new CloseCommand(this);
			this.Construction = new ConstructionCommand(this);
			this.Reform = new ReformCommand(this);
			this.LineDiagram = new LineDiagramCommand(this);
			this.gameInfo = gameInfo;

			gameInfo.lastSeenLine = line;
		}

		/// <summary>
		/// 閉じるコマンド
		/// </summary>
		public class CloseCommand : CommandBase
		{
			private LineInfoViewModel vm;

			public CloseCommand(LineInfoViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return true;
			}

			public override void Execute(object parameter)
			{
				((Window)vm.page.Parent).Close();
			}
		}

		/// <summary>
		/// 路線建造コマンド
		/// </summary>
		public class ConstructionCommand : CommandBase
		{
			private LineInfoViewModel vm;

			public ConstructionCommand(LineInfoViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return true;
			}

			public override void Execute(object parameter)
			{
				var window = new ConstructWindow(vm.line, vm.gameInfo);
				window.ShowDialog();
				vm.InvokeAllNotify();
			}
		}

		/// <summary>
		/// 路線改造コマンド
		/// </summary>
		public class ReformCommand : CommandBase
		{
			private LineInfoViewModel vm;

			public ReformCommand(LineInfoViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.line.IsExist;
			}

			public override void Execute(object parameter)
			{
				var window = new ReformWindow(vm.line, vm.gameInfo);
				window.ShowDialog();
				vm.InvokeAllNotify();
			}
		}

		/// <summary>
		/// 運行ダイヤ設定コマンド
		/// </summary>
		public class LineDiagramCommand : CommandBase
		{
			private LineInfoViewModel vm;

			public LineDiagramCommand(LineInfoViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.line.IsExist;
			}

			public override void Execute(object parameter)
			{
				var window = new LineDiagramSettingWindow(vm.line, vm.gameInfo);
				window.ShowDialog();
				vm.InvokeAllNotify();
			}
		}
	}
}
