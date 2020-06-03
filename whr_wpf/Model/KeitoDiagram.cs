using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using whr_wpf.Util;

namespace whr_wpf.Model
{
	/// <summary>
	/// 運転系統ダイアグラム
	/// シナリオver2以上
	/// </summary>
	[Serializable()]
	public class KeitoDiagram
	{
		/// <summary>
		/// 系統名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 開始駅
		/// </summary>
		public Station start;

		/// <summary>
		/// 終了駅
		/// </summary>
		public Station end;

		/// <summary>
		/// 投入編成
		/// </summary>
		public IComposition useComposition;

		/// <summary>
		/// 投入編成数
		/// </summary>
		public int useCompositionNum;

		/// <summary>
		/// 日間運行本数
		/// </summary>
		public int runningPerDay;

		/// <summary>
		/// 経由ルート
		/// </summary>
		public List<Line> route;

		/// <summary>
		/// 日間乗車可能数
		/// </summary>
		public int PassengerCapacityPerDay => LogicUtil.CalcMaximumCapacity(useComposition, runningPerDay);

		/// <summary>
		/// 指定された編成を受け入れ可能か検証
		/// </summary>
		/// <param name="composition">設定したい編成</param>
		/// <param name="gameInfo">ゲーム情報</param>
		public void ValidateCompositionAcceptable(IComposition composition, GameInfo gameInfo)
		{
			if (composition is null)
			{
				throw new ArgumentNullException(nameof(composition));
			}

			if (gameInfo is null)
			{
				throw new ArgumentNullException(nameof(gameInfo));
			}

			route.ForEach(line => line.ValidateCompositionAcceptable(composition, gameInfo));
		}

		/// <summary>
		/// 属する路線は建造済みか
		/// </summary>
		public bool IsExist => route.Where(line => line.IsExist == false).Count() == 0;

		/// <summary>
		/// ダイアリセット
		/// </summary>
		public void DiagramReset()
		{
			useComposition.Release(useCompositionNum);
			useComposition = null;
			useCompositionNum = 0;
			runningPerDay = 0;
		}

		/// <summary>
		/// 未敷設区間を含むか
		/// </summary>
		/// <returns></returns>
		public bool DoesIncludeUnlaidSections() => route.Where(line => line.IsExist == false).Count() > 0;

		/// <summary>
		/// リニアが混在
		/// </summary>
		public bool IsMixedLinear()
		{
			return (route.Where(line => line.Type == RailTypeEnum.LinearMotor).Count() > 0)
				&& (route.Where(line => line.Type == RailTypeEnum.Iron).Count() > 0);
		}

		/// <summary>
		/// 全てリニアで統一されているか
		/// </summary>
		/// <returns></returns>
		public bool IsAllLinear()
		{
			return (route.Where(line => line.Type == RailTypeEnum.LinearMotor).Count() > 0)
				&& (route.Where(line => line.Type == RailTypeEnum.Iron).Count() == 0);
		}

		/// <summary>
		/// 全て電化されているか
		/// </summary>
		/// <returns></returns>
		public bool IsAllElectrified()
		{
			return (route.Where(line => line.Type == RailTypeEnum.Iron && !(bool)line.IsElectrified).Count() == 0)
				&& (route.Where(line => line.Type == RailTypeEnum.Iron && (bool)line.IsElectrified).Count() > 0);
		}

		/// <summary>
		/// 系統の軌間が何で統一されているか
		/// </summary>
		/// <returns>混在、全区間が鉄軌道じゃない,未建造ならnull</returns>
		public RailGaugeEnum? JudgeDiagramGauge()
		{
			bool isExistNarrow = route.Where(line => line.Type == RailTypeEnum.Iron && line.gauge == RailGaugeEnum.Narrow).Count() > 0;
			bool isExistRegular = route.Where(line => line.Type == RailTypeEnum.Iron && line.gauge == RailGaugeEnum.Regular).Count() > 0;
			if (isExistNarrow && !isExistRegular) { return RailGaugeEnum.Narrow; }
			if (!isExistNarrow && isExistRegular) { return RailGaugeEnum.Regular; }
			return isExistNarrow && isExistRegular ? null : (RailGaugeEnum?)null;
		}

		/// <summary>
		/// この系統を除いた最大運行可能本数を計算
		/// </summary>
		/// <param name="genkaiKyoyo">限界許容本数</param>
		/// <returns>運行可能本数 ネックとなっている路線</returns>
		public (int maximumRunningPerDay, Line bottleNeck) CalcMaximumRunningPerDay(int genkaiKyoyo)
		{
			Line bottleNeckLine = route.OrderBy(line => line.CalcExcessCapacityWithoutSpecifiedKeito(this, false, genkaiKyoyo)).FirstOrDefault();
			return (bottleNeckLine.CalcExcessCapacityWithoutSpecifiedKeito(this, true, genkaiKyoyo), bottleNeckLine);
		}

		/// <summary>
		/// 系統の所要時間計算
		/// </summary>
		/// <param name="composition">計算に使用する編成</param>
		/// <returns>所要分数</returns>
		public int CalcRequieredMinutes(IComposition composition) => route.Select(line => line.CalcRequieredMinutes(composition)).Sum();

		/// <summary>
		/// 編成と運行本数を指定して乗車率予想
		/// </summary>
		/// <param name="composition"></param>
		/// <param name="runningPerDay"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public Dictionary<Line, int> PredicateJosharitsu(IComposition composition, int runningPerDay)
		{
			//予測前にこの系統の設定を一時的に書き換えて予測して戻す
			var currentComp = this.useComposition;
			var currentRunningPerday = this.runningPerDay;

			useComposition = composition;
			this.runningPerDay = runningPerDay;
			Dictionary<Line, int> joshaDict = route.ToDictionary(line => line, line => line.Josharitsu);
			this.useComposition = currentComp;
			this.runningPerDay = currentRunningPerday;

			return joshaDict;
		}

		/// <summary>
		/// 運行したい本数に合わせて路線のダイアを変更
		/// 一旦、排他制御ながら副作用がないようにする
		/// 一時的に副作用があるので排他処理が必要
		/// </summary>
		/// <param name="runningPerDay">運行させたい本数</param>
		/// <param name="gameInfo">ゲーム情報</param>
		/// <returns>true：ダイア変更して/しなくても走れる  false:変更しても運行本数が限界</returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public (bool isAcceptableFreq, ImmutableDictionary<Line, DiagramType> lineDiagramPairs) JudgeDiagramForRunningPerDay(int runningPerDay, GameInfo gameInfo)
		{
			//現在の各路線のダイヤ設定を温存
			ImmutableDictionary<Line, DiagramType> originalDiagram =
				route.ToImmutableDictionary(line => line, line => line.diagram);

			//運行したい本数より余剰本数が下回っていればダイヤ変更し余剰を増やす
			while (true)
			{
				(int maximumRunningPerDay, Line bottleNeck) = CalcMaximumRunningPerDay(gameInfo.genkaikyoyo);
				if (maximumRunningPerDay < runningPerDay)
				{
					//本当は副作用が出ないようにしたい
					switch (bottleNeck.diagram)
					{
						case DiagramType.LimittedExpressPrior:
							bottleNeck.diagram = DiagramType.Regular;
							break;
						case DiagramType.Regular:
							bottleNeck.diagram = DiagramType.OverCrowded;
							break;
						case DiagramType.OverCrowded:
							bottleNeck.diagram = DiagramType.Parallel;
							break;
						case DiagramType.Parallel:
							//ダイヤ設定を復元
							RestoreLinesDiagram(originalDiagram);
							return (false, null);
					}
				}
				else
				{
					//変更結果を抽出
					ImmutableDictionary<Line, DiagramType> changedResult =
						route.ToImmutableDictionary(line => line, line => line.diagram);

					//ダイヤ設定を復元
					RestoreLinesDiagram(originalDiagram);
					return (true, changedResult);
				}
			}

			///<summary>ダイアグラム復元</summary>
			void RestoreLinesDiagram(ImmutableDictionary<Line, DiagramType> originalDiagram)
			{
				route.ForEach(line => line.diagram = originalDiagram[line]);
			}
		}

		/// <summary>
		/// 系統ダイヤの編成の設定と、傘下の路線のダイヤ変更
		/// </summary>
		/// <param name="newComposition"></param>
		/// <param name="runningPerDay"></param>
		/// <param name="gameInfo"></param>
		/// 
		public void SettingComposition(IComposition newComposition, int runningPerDay, GameInfo gameInfo)
		{
			if (gameInfo is null)
			{
				throw new ArgumentNullException(nameof(gameInfo));
			}

			if (newComposition is null)
			{
				throw new ArgumentNullException(nameof(newComposition));
			}

			//先に編成が設定できるかチェック　そうしないと編成確保時に不整合が起きる
			(bool isAcceptableFreq, ImmutableDictionary<Line, DiagramType> lineDiagramPairs) = JudgeDiagramForRunningPerDay(runningPerDay, gameInfo);
			if (!isAcceptableFreq) { throw new InvalidOperationException("この路線には編成が設定できません"); }

			ValidateCompositionAcceptable(newComposition, gameInfo);

			//現在の各路線のダイヤ設定を保存
			ImmutableDictionary<Line, DiagramType> originalDiagram = route.ToImmutableDictionary(line => line, line => line.diagram);

			int newUseCompositionNum = newUseCompositionNum = CalcUseCompositionNum(newComposition, runningPerDay, lineDiagramPairs);

			if (newComposition.HeldUnits < CalcMissingCompositions(newComposition, runningPerDay, lineDiagramPairs))
			{
				//例外発生時は復元
				//route.ForEach(line => line.diagram = originalDiagram[line]);
				throw new InvalidOperationException("編成が足りません");
			}

			ReleaseComposition();
			newComposition.Use(newUseCompositionNum);
			useComposition = newComposition;
			useCompositionNum = newUseCompositionNum;
			this.runningPerDay = runningPerDay;
			route.ForEach(line => line.diagram = lineDiagramPairs[line]);
		}

		/// <summary>
		/// 不足している編成数を計算
		/// </summary>
		/// <param name="newComposition">設定したい編成</param>
		/// <param name="runningPerDay">運行本数</param>
		/// <param name="lineDiagramPairs">路線と適したダイアの組</param>
		/// <returns></returns>
		public int CalcMissingCompositions(IComposition newComposition, int runningPerDay, ImmutableDictionary<Line, DiagramType> lineDiagramPairs)
		{
			int newUseCompNum = CalcUseCompositionNum(newComposition, runningPerDay, lineDiagramPairs);

			if (useComposition == newComposition)
			{
				//現行運用分を引き継いで使用パターン
				int missing = newUseCompNum - useCompositionNum - newComposition.HeldUnits;
				return missing > 0 ? missing : 0;
			}
			else
			{
				//編成変更パターン
				return newUseCompNum - newComposition.HeldUnits;
			}
		}

		/// <summary>
		/// 運行本数から必要な編成数を割り出す
		/// </summary>
		/// <param name="newComposition"></param>
		/// <param name="runningPerDay"></param>
		/// <param name="lineDiagramPairs">路線と適したダイアの組</param>
		/// <returns></returns>
		public int CalcUseCompositionNum(IComposition newComposition, int runningPerDay, ImmutableDictionary<Line, DiagramType> lineDiagramPairs)
		{
			if (newComposition is null) { return 0; }

			//投入編成数を徐々に増やしていき、その投入編成数で引数の運行本数が最初に超えた投入編成数を必要な編成数とする
			for (int tempCompositionNum = 0; tempCompositionNum < int.MaxValue; tempCompositionNum++)
			{
				int tempRunningPerDay = CalcRunningPerDay(newComposition, tempCompositionNum, lineDiagramPairs);
				if (tempRunningPerDay >= runningPerDay) { return tempCompositionNum; }
			}
			return 0;
		}

		/// <summary>
		/// 系統運行本数算出
		/// </summary>
		/// <param name="newComposition">編成</param>
		/// <param name="useCompositionNum">投入編成数</param>
		/// <param name="lineDiagramPairs">路線と適したダイアの組</param>
		/// <returns></returns>
		private int CalcRunningPerDay(IComposition newComposition, int useCompositionNum, ImmutableDictionary<Line, DiagramType> lineDiagramPairs)
		{
			int requireMinutes = route.Select(line => line.CalcRequieredMinutes(newComposition, lineDiagramPairs[line])).Sum();
			if (requireMinutes == 0) { return 0; }
			return 540 * useCompositionNum / requireMinutes;
		}

		/// <summary>
		/// 投入していた編成の解放
		/// </summary>
		private void ReleaseComposition()
		{
			if (useComposition == null)
			{
				return;
			}
			useComposition.Release(useCompositionNum);
			useComposition = null;
			useCompositionNum = 0;
			runningPerDay = 0;
		}
	}
}
