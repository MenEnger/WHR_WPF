using System;
using System.Collections.Generic;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View.Line;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// 路線運行ダイヤ設定VM
	/// </summary>
	class LineDiagramSettingViewModel : ViewModelBase
	{
		private readonly Line line;
		private GameInfo gameInfo;

		public List<IComposition> CompositionList { get; set; }

		private IComposition _composition;
		public IComposition Composition
		{
			get => _composition;
			set
			{
				_composition = value;
				try
				{
					line.ValidateCompositionAcceptable(value, gameInfo);
					ErrorMsg = "";
				}
				catch (CompositionNotAppliedException e)
				{
					ErrorMsg = e.Message;
				}
				catch (ArgumentNullException)
				{
					ErrorMsg = "編成が未選択です";
				}
				InvokeAllNotify();
			}
		}

		private int _runningPerDay;
		public int RunningPerDay
		{
			get => _runningPerDay; set
			{
				if (value >= 0)
				{
					_runningPerDay = value;
				}
				InvokeAllNotify();
			}
		}

		public string ErrorMsg { get; set; }
		public string BestSpeed => $"最高速度　{Composition?.BestSpeed.ToString() ?? ""}km/h";
		public string Gauge => Composition != null && Composition.Gauge != null ? $"軌間　{Composition.Gauge?.ToName() ?? ""}" : "";
		public string PowerSource => Composition != null ? $"動力　{Composition.Power.ToName()}" : "";
		public string CarNum => Composition != null ? $"編成両数　{((Composition is DefautltComposition dComp) ? dComp.CarCount : Composition.CarCount)}" : "";
		public string HeldUnits => Composition != null ? $"余剰編成数　{Composition.HeldUnits}" : "";
		public string Price => Composition != null ? $"編成価格　{LogicUtil.AppendMoneyUnit(Composition.Price)}" : "";
		public string UseCompositionNum => $"投入編成数　{CalcUseCompositionNum()}編成";

		public string RequiredMinutes => $"所要時間　{(Composition is null ? "-" : CalcRequieredMinutes().ToString())}分";
		public string Josharitsu
		{
			get
			{
				int josharitsu = line.PredicateJosharitsu(Composition, RunningPerDay);
				if (josharitsu > 0)
				{
					return $"予想乗車率　{josharitsu}%";
				}
				else { return "乗車率予想不可能"; }
			}
		}
		public string DiagramNum => $"系統列車　{line.TripsOfBelongingDiagrams}本/日";
		public string DiagramType
		{
			get
			{
				DiagramType suitableDiagram = JudgementSuitableDiagram();
				return suitableDiagram == Model.DiagramType.None ? "運行本数が限界です" : suitableDiagram.ToName();
			}
		}

		public ICommand Kettei { get; set; }
		public ICommand Up { get; set; }
		public ICommand Down { get; set; }
		public ICommand SpeedUp { get; set; }
		public ICommand Close { get; set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="gameInfo"></param>
		/// <param name="line"></param>
		/// <param name="window"></param>
		public LineDiagramSettingViewModel(GameInfo gameInfo, Line line, LineDiagramSettingWindow window)
		{
			this.gameInfo = gameInfo;
			CompositionList = gameInfo.compositions;
			this.line = line;
			this.Composition = line.useComposition;
			this.window = window;
			RunningPerDay = line.runningPerDay;

			Kettei = new KetteiCommand(this);
			Up = new HonsuuUpCommand(this);
			Down = new HonsuuDownCommand(this);
			SpeedUp = new SpeedUpCommand(this);
			Close = new CancelCommand(this);
		}

		/// <summary>
		/// 運行本数に適したダイアグラムを判定
		/// </summary>
		/// <returns>ダイアグラム</returns>
		private DiagramType JudgementSuitableDiagram() => line.JudgeSuitableDiagram(RunningPerDay, gameInfo.genkaikyoyo);

		/// <summary>
		/// 編成&ダイヤが設定できるか
		/// </summary>
		/// <returns></returns>
		private bool CanCompositonSettting()
		{
			if (Composition == null) { return false; }

			if (JudgementSuitableDiagram() == Model.DiagramType.None) { return false; }

			try
			{
				line.ValidateCompositionAcceptable(Composition, gameInfo);
			}
			catch (CompositionNotAppliedException)
			{
				return false;
			}
			return line.CanCompositionSetting();
		}

		/// <summary>
		/// 不足した編成の購入コスト計算
		/// </summary>
		/// <returns></returns>
		private long CalcCompositionPurchaseCost() => Composition.Price * CalcMissingCompositions();

		/// <summary>
		/// 不足した編成を購入
		/// </summary>
		private void PurchaseMissingCompositions() => Composition.Purchase(gameInfo, CalcMissingCompositions());

		/// <summary>
		/// 編成を設定
		/// </summary>
		private void CompositionSetting() => line.SettingComposition(Composition, RunningPerDay, JudgementSuitableDiagram(), gameInfo);

		/// <summary>
		/// 投入編成数を計算
		/// </summary>
		/// <returns></returns>
		private int CalcUseCompositionNum()
		{
			DiagramType diagramType = JudgementSuitableDiagram();
			return diagramType == Model.DiagramType.None
				? 0
				: Composition is null ? 0 : line.CalcUseCompositionNum(Composition, RunningPerDay, diagramType);
		}

		/// <summary>
		/// 不足している編成数を計算
		/// </summary>
		/// <returns></returns>
		private int CalcMissingCompositions() => line.CalcMissingCompositions(Composition, RunningPerDay, JudgementSuitableDiagram());

		/// <summary>
		/// 所要時間計算
		/// </summary>
		/// <returns>所要分数</returns>
		public int CalcRequieredMinutes()
		{
			DiagramType diagramType = JudgementSuitableDiagram();
			return diagramType == Model.DiagramType.None ? 0 : line.CalcRequieredMinutes(Composition, diagramType);
		}

		private bool CanSpeedUp() => line.CanSpeedUp();

		private long CalcUppedSpeed() => line.GetSpeedUppedBestSpeed();

		public long CalcSpeedUpCost() => line.CalcSpeedUpCost(gameInfo);

		public void ExecuteSpeedUp()
		{
			line.SpeedUp(gameInfo);
			InvokeAllNotify();
		}

		private void CloseWindow() => window.Close();

		/// <summary>
		/// 決定ボタン
		/// </summary>
		public class KetteiCommand : CommandBase
		{
			LineDiagramSettingViewModel vm;
			public KetteiCommand(LineDiagramSettingViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => vm.CanCompositonSettting();
			public override void Execute(object parameter)
			{
				//編成が足りるか
				if (vm.CalcMissingCompositions() > 0)
				{
					//不足分購入
					string text = $"不足する編成を購入するには{LogicUtil.AppendMoneyUnit(vm.CalcCompositionPurchaseCost())}かかります。よろしいですか？";
					ExecuteDelegete exec = new ExecuteDelegete(vm.PurchaseMissingCompositions);
					vm.ExecuteWithMoney(text, exec);
				}

				//ダイヤ設定
				vm.ExecuteWithMoney(new ExecuteDelegete(vm.CompositionSetting));
			}
		}

		/// <summary>
		/// 運行本数UP
		/// </summary>
		public class HonsuuUpCommand : CommandBase
		{
			LineDiagramSettingViewModel vm;
			public HonsuuUpCommand(LineDiagramSettingViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.RunningPerDay++;
		}

		/// <summary>
		/// 運行本数Down
		/// </summary>
		public class HonsuuDownCommand : CommandBase
		{
			LineDiagramSettingViewModel vm;
			public HonsuuDownCommand(LineDiagramSettingViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.RunningPerDay--;
		}

		/// <summary>
		/// スピードアップ
		/// </summary>
		public class SpeedUpCommand : CommandBase
		{
			private LineDiagramSettingViewModel vm;

			public SpeedUpCommand(LineDiagramSettingViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => vm.CanSpeedUp();

			public override void Execute(object parameter)
			{
				string text = $"最高速度{vm.CalcUppedSpeed()}km/hにアップすると{LogicUtil.AppendMoneyUnit(vm.CalcSpeedUpCost())}かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteSpeedUp);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// キャンセル
		/// </summary>
		public class CancelCommand : CommandBase
		{
			LineDiagramSettingViewModel vm;
			public CancelCommand(LineDiagramSettingViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.CloseWindow();
		}
	}
}
