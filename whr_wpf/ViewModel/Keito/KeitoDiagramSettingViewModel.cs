using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View.Line;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// 系統運行ダイヤ設定VM
	/// </summary>
	class KeitoDiagramSettingViewModel : ViewModelBase
	{
		private readonly KeitoDiagram keitoDiagram;
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
					keitoDiagram.ValidateCompositionAcceptable(value, gameInfo);
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
					(bool isAcceptableFreq, _) = JudgeDiagramForRunningPerDay(value);
					if (!isAcceptableFreq)
					{
						ErrorMsg = "運行本数が限界です";
					}
				}
				InvokeAllNotify();
			}
		}

		/// <summary>
		/// 運行したい本数に合わせて路線のダイアを変更
		/// 一旦、排他制御ながら副作用がないようにする
		/// 一時的に副作用があるので排他処理が必要
		/// </summary>
		/// <param name="runningPerDay">運行させたい本数</param>
		/// <returns>true：ダイア変更して/しなくても走れる  false:ダイア変更しても運行本数が限界</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		private (bool isAcceptableFreq, ImmutableDictionary<Line, DiagramType> lineDiagramPairs) JudgeDiagramForRunningPerDay(int runningPerDay)
		{
			return keitoDiagram.JudgeDiagramForRunningPerDay(runningPerDay, gameInfo);
		}

		public string ErrorMsg { get; set; }
		public string UseCompositionNum => $"投入編成数　{CalcUseCompositionNum()}編成";
		public string TransportCapacity => $"輸送力　{LogicUtil.CalcMaximumCapacity(Composition, RunningPerDay)}00人/週";
		public string RequiredMinutes => $"所要時間　{(Composition is null ? "-" : CalcRequieredMinutes().ToString())}分";
		public string BestSpeed => $"最高速度　{Composition?.BestSpeed.ToString() ?? ""}km/h";
		public string Gauge => Composition != null && Composition.Gauge != null ? $"軌間　{Composition.Gauge?.ToName() ?? ""}" : "";
		public string PowerSource => Composition != null ? $"動力　{Composition.Power.ToName()}" : "";
		public string CarNum => Composition != null ? $"編成両数　{((Composition is DefautltComposition dComp) ? dComp.CarCount : Composition.CarCount)}" : "";
		public string HeldUnits => Composition != null ? $"余剰編成数　{Composition.HeldUnits}" : "";
		public string Price => Composition != null ? $"編成価格　{Composition.Price}拾万円" : "";
		public string Josharitsu
		{
			get
			{
				string msg = "予想乗車率\n";
				Dictionary<Line, int> josharitsuDict = keitoDiagram.PredicateJosharitsu(Composition, RunningPerDay);
				var josharitsuMsgs = josharitsuDict.Select(entry => $"{entry.Key.Start.Name}～{entry.Key.End.Name}　{entry.Value}%").ToArray();
				return msg + string.Join('\n', josharitsuMsgs);
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
		public KeitoDiagramSettingViewModel(GameInfo gameInfo, KeitoDiagram keitoDiagram, KeitoDiagramSettingWindow window)
		{
			this.gameInfo = gameInfo;
			CompositionList = gameInfo.compositions;
			this.keitoDiagram = keitoDiagram;
			this.Composition = keitoDiagram.useComposition;
			this.window = window;
			RunningPerDay = keitoDiagram.runningPerDay;

			Kettei = new KetteiCommand(this);
			Up = new HonsuuUpCommand(this);
			Down = new HonsuuDownCommand(this);
			SpeedUp = new SpeedUpCommand(this);
			Close = new CancelCommand(this);

			gameInfo.lastSeenKeito = keitoDiagram;
		}

		/// <summary>
		/// 運行本数に適したダイアグラムを判定
		/// </summary>
		/// <returns>ダイアグラム</returns>
		// private DiagramType JudgementSuitableDiagram() => line.JudgeSuitableDiagram(RunningPerDay, gameInfo.genkaikyoyo);

		/// <summary>
		/// 編成&ダイヤが設定できるか
		/// </summary>
		/// <returns>true:可能 false:不可能</returns>
		private bool CanCompositonSettting()
		{
			if (Composition == null) { return false; }

			var (isAcceptableFreq, _) = JudgeDiagramForRunningPerDay(RunningPerDay);
			if (!isAcceptableFreq) { return false; }

			try
			{
				keitoDiagram.ValidateCompositionAcceptable(Composition, gameInfo);
			}
			catch (CompositionNotAppliedException)
			{
				return false;
			}
			return true;
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
		/// 編成と運行本数を設定
		/// </summary>
		private void CompositionSetting()
		{
			if (!CanCompositonSettting())
			{
				return;
			}

			keitoDiagram.SettingComposition(Composition, RunningPerDay, gameInfo);
		}

		/// <summary>
		/// 投入編成数を計算
		/// </summary>
		/// <returns></returns>
		private int CalcUseCompositionNum()
		{
			(bool _, ImmutableDictionary<Line, DiagramType> lineDiagramPairs) = JudgeDiagramForRunningPerDay(RunningPerDay);
			if (lineDiagramPairs is null) { return 0; }
			return keitoDiagram.CalcUseCompositionNum(Composition, RunningPerDay, lineDiagramPairs);
		}

		/// <summary>
		/// 不足している編成数を計算
		/// </summary>
		/// <returns></returns>
		private int CalcMissingCompositions()
		{
			(bool _, ImmutableDictionary<Line, DiagramType> lineDiagramPairs) = JudgeDiagramForRunningPerDay(RunningPerDay);
			if (lineDiagramPairs is null) { return 0; }
			return keitoDiagram.CalcMissingCompositions(Composition, RunningPerDay, lineDiagramPairs);
		}

		/// <summary>
		/// 系統の所要時間計算
		/// </summary>
		/// <returns>所要分数</returns>
		public int CalcRequieredMinutes() => keitoDiagram.CalcRequieredMinutes(Composition);

		private bool CanSpeedUp() => true; //スピードアップは未実装

		//private long CalcUppedSpeed() => line.GetSpeedUppedBestSpeed();

		//public long CalcSpeedUpCost() => line.CalcSpeedUpCost(gameInfo);

		/*
		public void ExecuteSpeedUp()
		{
			line.SpeedUp(gameInfo);
			InvokeAllNotify();
		}*/

		private void CloseWindow() => window.Close();

		/// <summary>
		/// 決定ボタン
		/// </summary>
		public class KetteiCommand : CommandBase
		{
			KeitoDiagramSettingViewModel vm;
			public KetteiCommand(KeitoDiagramSettingViewModel vm) => this.vm = vm;
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
			KeitoDiagramSettingViewModel vm;
			public HonsuuUpCommand(KeitoDiagramSettingViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.RunningPerDay++;
		}

		/// <summary>
		/// 運行本数Down
		/// </summary>
		public class HonsuuDownCommand : CommandBase
		{
			KeitoDiagramSettingViewModel vm;
			public HonsuuDownCommand(KeitoDiagramSettingViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.RunningPerDay--;
		}

		/// <summary>
		/// スピードアップ
		/// </summary>
		public class SpeedUpCommand : CommandBase
		{
			private KeitoDiagramSettingViewModel vm;

			public SpeedUpCommand(KeitoDiagramSettingViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => vm.CanSpeedUp();

			public override void Execute(object parameter)
			{
				//系統のスピードアップは未実装
				/*
				string text = $"最高速度{vm.CalcUppedSpeed()}km/hにアップすると{vm.CalcSpeedUpCost()}拾万円かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteSpeedUp);
				vm.ExecuteWithMoney(text, exec, true);
				*/
			}
		}

		/// <summary>
		/// キャンセル
		/// </summary>
		public class CancelCommand : CommandBase
		{
			KeitoDiagramSettingViewModel vm;
			public CancelCommand(KeitoDiagramSettingViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.CloseWindow();
		}
	}
}
