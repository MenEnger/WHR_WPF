using System.Linq;
using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.View.Line;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// 系統ダイヤ情報ダイアログのVM
	/// </summary>
	public class DiagramInfoViewModel : ViewModelBase
	{
		public KeitoDiagram keito;
		private DiagramInfoPage page;
		private GameInfo gameInfo;

		public string Section => $"区間　{keito.start.Name}～{keito.end.Name}";

		public string DiagramSectionInfo
		{
			get
			{
				if (DoesIncludeUnlaidSections()) { return "未敷設区間あり"; }
				if (IsMixedLinear()) { return "リニアが混在"; }
				if (IsAllLinear()) { return "全てリニアで統一"; }
				string msg = IsAllElectrified() ? "すべての電化が完了" : "非電化区間あり";
				msg += "\n";
				msg += (JudgeDiagramGauge()) switch
				{
					RailGaugeEnum.Narrow => "狭軌で統一",
					RailGaugeEnum.Regular => "標準軌で統一",
					_ => "狭軌と標準軌が混在",
				};
				return msg;
			}
		}

		public string UseComposition => $"使用車両　{keito.useComposition?.Name ?? "" }";
		public string UseCompositionNum => $"投入数　{keito.useCompositionNum}編成";
		public string RunningPerDay => $"運行数　{keito.runningPerDay}本/日";

		/// <summary>
		/// 未敷設区間を含むか
		/// </summary>
		/// <returns></returns>
		private bool DoesIncludeUnlaidSections() => keito.DoesIncludeUnlaidSections();

		/// <summary>
		/// リニアが混在
		/// </summary>
		private bool IsMixedLinear() => keito.IsMixedLinear();

		/// <summary>
		/// 全てリニアで統一されているか
		/// </summary>
		/// <returns></returns>
		private bool IsAllLinear() => keito.IsAllLinear();

		/// <summary>
		/// 全て電化されているか
		/// </summary>
		/// <returns></returns>
		private bool IsAllElectrified() => keito.IsAllElectrified();

		/// <summary>
		/// 系統の軌間が何で統一されているか
		/// </summary>
		/// <returns>混在or全区間が鉄軌道じゃないならnull</returns>
		private RailGaugeEnum? JudgeDiagramGauge()
		{
			bool narrow = keito.route.Where(line => line.Type == RailTypeEnum.Iron && line.gauge == RailGaugeEnum.Narrow).Count() > 0;
			bool regular = keito.route.Where(line => line.Type == RailTypeEnum.Iron && line.gauge == RailGaugeEnum.Regular).Count() > 0;
			if (narrow && !regular) { return RailGaugeEnum.Narrow; }
			if (!narrow && regular) { return RailGaugeEnum.Regular; }
			return narrow && regular ? null : (RailGaugeEnum?)null;
		}


		public ICommand Close { get; set; }
		public ICommand DiagramSetting { get; set; }
		public ICommand DiagramLineInfo { get; set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="diagram">選択状態になっているダイアグラム</param>
		/// <param name="diagramInfoPage">ページ</param>
		public DiagramInfoViewModel(KeitoDiagram diagram, DiagramInfoPage diagramInfoPage, GameInfo gameInfo)
		{
			this.keito = diagram;
			this.page = diagramInfoPage;
			this.gameInfo = gameInfo;
			this.Close = new CloseCommand(this);
			this.DiagramLineInfo = new BelongsLinesInfoShowCommand(this);
			this.DiagramSetting = new DiagramCommand(this);
		}

		/// <summary>
		/// 閉じるコマンド
		/// </summary>
		public class CloseCommand : CommandBase
		{
			private DiagramInfoViewModel vm;

			public CloseCommand(DiagramInfoViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => true;

			public override void Execute(object parameter) => ((Window)vm.page.Parent).Close();
		}

		/// <summary>
		/// 各路線情報
		/// </summary>
		public class BelongsLinesInfoShowCommand : CommandBase
		{
			private DiagramInfoViewModel vm;

			public BelongsLinesInfoShowCommand(DiagramInfoViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => true;

			public override void Execute(object parameter)
			{
				var rows = vm.keito.route.Select(line =>
				{
					string section = $"{line.Name}({line.Start.Name}～{line.End.Name})";
					string status = $"{(!line.IsExist ? "未敷設" : FormatLineStatus(line))}";
					return $"{section}: {status}";
				}).ToList();
				string txt = string.Join('\n', rows);
				MessageBox.Show(txt);
			}

			/// <summary>
			/// 路線情報の組み立て
			/// </summary>
			/// <param name="line"></param>
			/// <returns></returns>
			string FormatLineStatus(Line line)
			{
				int excess = line.CalcExcessCapacity(vm.gameInfo.Kamotu != KamotsuEnum.Nothing, vm.gameInfo.genkaikyoyo);
				string t = $"乗車率{line.Josharitsu}%　余剰本数{excess}本　";
				if (vm.gameInfo.Kamotu != KamotsuEnum.Nothing) { t += $"貨物{ line.kamotsuNumLastWeek} 本"; }
				return t;
			}
		}

		/// <summary>
		/// 運行系統ダイヤ設定
		/// </summary>
		public class DiagramCommand : CommandBase
		{
			private DiagramInfoViewModel vm;

			public DiagramCommand(DiagramInfoViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => vm.keito.IsExist;

			public override void Execute(object parameter)
			{
				var window = new KeitoDiagramSettingWindow(vm.keito, vm.gameInfo);
				window.ShowDialog();
				vm.InvokeAllNotify();
			}
		}
	}


}
