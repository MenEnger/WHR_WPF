using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using whr_wpf.Util;

namespace whr_wpf.Model
{
	/// <summary>
	/// 路線
	/// </summary>
	[Serializable()]
	public class Line : INotifyPropertyChanged
	{
		/// <summary>
		/// 路線名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 敷設済みか
		/// </summary>
		public bool IsExist { get; set; }

		/// <summary>
		/// 路線のタイプ
		/// </summary>
		public RailTypeEnum Type { get; set; }

		/// <summary>
		/// 軌間幅
		/// </summary>
		public RailGaugeEnum gauge;

		/// <summary>
		/// 電化路線か
		/// </summary>
		public bool? IsElectrified { get; set; }

		/// <summary>
		/// 最高速度 lbs
		/// </summary>
		public int bestSpeed;

		/// <summary>
		/// 最高速度改造回数
		/// </summary>
		public int bestSpeedUpKaisu;

		/// <summary>
		/// 路線数 las　1=単線　2=複線　4～=複々線～
		/// </summary>
		public int LaneNum
		{
			get => laneNum; set
			{
				laneNum = value;
				OnPropertyChanged();
			}
		}
		private int laneNum;

		/// <summary>
		/// 待避線
		/// </summary>
		public TaihisenEnum taihisen = TaihisenEnum.Every20km;

		/// <summary>
		/// 投入編成
		/// </summary>
		public IComposition useComposition;

		/// <summary>
		/// 投入編成数
		/// </summary>
		public int useCompositionNum;

		/// <summary>
		/// 一日あたり運行本数 lcr
		/// </summary>
		public int runningPerDay;

		/// <summary>
		/// ダイア
		/// </summary>
		public DiagramType diagram = DiagramType.LimittedExpressPrior;

		/// <summary>
		/// 定着率 lwe
		/// </summary>
		public int retentionRate = 10000;  //デフォ10000

		/// <summary>
		/// 先週の乗車数 lus
		/// </summary>
		public int passengersLastWeek;

		/// <summary>
		/// 総合収支
		/// </summary>
		public long totalBalance;

		/// <summary>
		/// 先週のその路線の収入状況
		/// </summary>
		public long incomeLastWeek;

		/// <summary>
		/// 先週のその路線の支出
		/// </summary>
		public long outlayLastWeek;

		/// <summary>
		/// 先週の貨物の本数 lkr
		/// </summary>
		public int kamotsuNumLastWeek;

		/// <summary>
		/// 輸送量限界を超えたか
		/// </summary>
		public bool isOverCapacity;

		/// <summary>
		/// 始点
		/// </summary>
		public Station Start { get; set; }

		/// <summary>
		/// 終点
		/// </summary>
		public Station End { get; set; }

		/// <summary>
		/// 路線の位置づけ
		/// </summary>
		public LineGrade grade;

		/// <summary>
		/// 路線属性タイプ
		/// </summary>
		public LinePropertyType propertyType;

		/// <summary>
		/// 路線が属している運行系統
		/// </summary>
		public List<KeitoDiagram> belongingKeitoDiagrams = new List<KeitoDiagram>();

		/// <summary>
		/// 距離 キロ
		/// </summary>
		public int Distance
		{
			get => distance;
			set
			{
				if (1 <= value && value <= 65535)
					distance = value;
				else throw new ArgumentException("距離は1~65535km");
			}
		}
		private int distance;

		/// <summary>
		/// 路線表示名
		/// </summary>
		public string Caption => $"{Name} {Start.Name}～{End.Name}";

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// 合計本数 hoc
		/// </summary>
		/// <param name="isIncludeKamotu">貨物を含むか</param>
		/// <returns>合計本数</returns>
		public int TotalNumberTrips(bool isIncludeKamotu)
		{
			//ダイヤ
			int diaCount = TripsOfBelongingDiagrams;

			if (isIncludeKamotu)
			{
				return diaCount + kamotsuNumLastWeek + runningPerDay;
			}
			else
			{
				return diaCount + runningPerDay;
			}
		}

		/// <summary>
		/// この路線が属する系統の日毎運行本数
		/// </summary>
		public int TripsOfBelongingDiagrams => belongingKeitoDiagrams.Select(keitoDia => keitoDia.runningPerDay).Sum();

		/// <summary>
		/// 路線の現在の限界本数 hons
		/// </summary>
		/// <param name="genkaiKyoyo">追加する許容路線本数</param>
		/// <returns></returns>
		public int GenkaiHonsuuUnderCurrent(int genkaiKyoyo) => CalcGenkaiHonsuu(this.diagram, genkaiKyoyo);

		/// <summary>
		/// 指定ダイアグラムでの路線の限界本数
		/// </summary>
		/// <param name="diagram"></param>
		/// <param name="genkaiKyoyo"></param>
		/// <returns></returns>
		private int CalcGenkaiHonsuu(DiagramType diagram, int genkaiKyoyo)
		{
			int honsu = 0;
			bool isOddLanes = this.LaneNum % 2 == 1;
			int lanePairsNum = this.LaneNum / 2;

			if (isOddLanes) honsu += 10;

			switch (diagram)
			{
				case DiagramType.None:
					honsu = 0;
					break;
				case DiagramType.Regular:
					honsu += lanePairsNum * 100 + (int)taihisen * 10 + genkaiKyoyo * this.LaneNum * 2;
					break;
				case DiagramType.LimittedExpressPrior:
					honsu += lanePairsNum * 80 + (int)taihisen * 10 + genkaiKyoyo * this.LaneNum - 10;
					break;
				case DiagramType.OverCrowded:
					honsu += lanePairsNum * 120 + (int)taihisen * 15 + genkaiKyoyo * this.LaneNum * 2;
					break;
				case DiagramType.Parallel:
					honsu += lanePairsNum * 310 + genkaiKyoyo * this.LaneNum * 2;
					if (isOddLanes)
					{
						honsu += (int)taihisen * 20;
					}
					break;
				default:
					throw new InvalidOperationException("ダイヤ算出式が存在しません");
			}
			if (Type == RailTypeEnum.LinearMotor)
			{
				honsu = honsu * 5 / 4;
			}
			return honsu;
		}

		/// <summary>
		/// 乗車率
		/// </summary>
		/// <returns></returns>
		public int Josharitsu
		{
			get
			{
				if (!IsExist) { return 0; }

				//乗車数÷(ダイヤの乗車可能数+路線の乗車可能数)
				int diagramCapacity = belongingKeitoDiagrams.Select(keito => keito.PassengerCapacityPerDay).Sum();
				int baseNum = diagramCapacity + PassengerCapacityPerDay;

				return CalcJosharitsuByCapacity(baseNum);
			}
		}

		/// <summary>
		/// 日間最大乗車数
		/// </summary>
		public int PassengerCapacityPerDay => LogicUtil.CalcMaximumCapacity(useComposition, runningPerDay);

		/// <summary>
		/// 建設コスト計算
		/// </summary>
		/// <param name="bestSpeed"></param>
		/// <param name="laneSu"></param>
		/// <param name="railType"></param>
		/// <param name="railGauge"></param>
		/// <param name="isElectrified"></param>
		/// <param name="taihisen"></param>
		/// <param name="gameInfo"></param>
		/// <returns></returns>
		public long CalcConstructCost(
			int bestSpeed, int laneSu,
			RailTypeEnum railType, RailGaugeEnum? railGauge, bool? isElectrified,
			TaihisenEnum taihisen,
			GameInfo gameInfo)
		{
			if (laneSu == 0) { return 0; }
			return LogicUtil.CalcLineConstructionCost(bestSpeed, laneSu, this.Distance, railType, railGauge, isElectrified, taihisen, this.propertyType, gameInfo);
		}

		/// <summary>
		/// 路線に関する資金の消費
		/// </summary>
		/// <param name="gameInfo"></param>
		/// <param name="cost"></param>
		private void SpendMoney(GameInfo gameInfo, long cost)
		{
			gameInfo.SpendMoney(cost);
			totalBalance -= cost;
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

		/// <summary>
		/// 走行ダイヤを系統ごとリセット
		/// </summary>
		private void DiagramReset()
		{
			ReleaseComposition();
			belongingKeitoDiagrams.ForEach((keito) => { keito.DiagramReset(); });
		}

		/// <summary>
		/// 路線建造
		/// </summary>
		/// <param name="bestSpeed"></param>
		/// <param name="railType"></param>
		/// <param name="isElectrified"></param>
		/// <param name="railGauge"></param>
		/// <param name="laneSu"></param>
		/// <param name="taihisenEnum"></param>
		public void Construct(int bestSpeed, RailTypeEnum railType, bool? isElectrified, RailGaugeEnum? railGauge, int laneSu, TaihisenEnum taihisenEnum, GameInfo gameInfo)
		{
			//お金チェックと消費
			gameInfo.SpendMoney(CalcConstructCost(bestSpeed,
									laneSu,
									railType,
									railGauge,
									isElectrified,
									taihisenEnum,
									gameInfo));

			this.IsExist = true;
			this.bestSpeed = bestSpeed;
			this.Type = railType;
			this.IsElectrified = isElectrified;
			this.gauge = railGauge.HasValue ? (RailGaugeEnum)railGauge : RailGaugeEnum.Narrow;
			this.LaneNum = laneSu;
			this.taihisen = taihisenEnum;
			this.totalBalance -= CalcConstructCost(bestSpeed, laneSu, railType, railGauge, isElectrified, taihisen, gameInfo);

			OnPropertyChanged(nameof(IsExist));
			OnPropertyChanged("");
		}

		/// <summary>
		/// スピードアップ可能か
		/// </summary>
		/// <returns></returns>
		public bool CanSpeedUp()
		{
			if (!IsExist) { return false; }
			if (bestSpeedUpKaisu == 5) { return false; }
			if (bestSpeed >= 990) { return false; }
			if (Type == RailTypeEnum.Iron)
			{
				if (bestSpeed >= 360) { return false; }
				if (gauge == RailGaugeEnum.Narrow && bestSpeed >= 300) { return false; }
			}
			return true;
		}

		/// <summary>
		/// スピードアップ増分の取得
		/// </summary>
		/// <returns>スピードアップ増分</returns>
		private int GetSpeedUpIncrement()
		{
			return bestSpeed < 200 ? 10 : 20;
		}

		/// <summary>
		/// スピードアップ後の路線最高速度
		/// </summary>
		/// <returns></returns>
		public int GetSpeedUppedBestSpeed()
		{
			return bestSpeed + GetSpeedUpIncrement();
		}

		/// <summary>
		/// スピードアップ費用の取得
		/// </summary>
		/// <param name="gameInfo">ゲーム情報</param>
		/// <returns>スピードアップ費用</returns>
		public long CalcSpeedUpCost(GameInfo gameInfo)
		{
			return LogicUtil.CalcLineConstructionCost(GetSpeedUppedBestSpeed() / 5 - 40, LaneNum, Distance, Type, gauge, IsElectrified, taihisen, propertyType, gameInfo);
		}

		/// <summary>
		/// 路線スピードアップ
		/// </summary>
		/// <param name="gameInfo">ゲーム情報</param>
		public void SpeedUp(GameInfo gameInfo)
		{
			if (!CanSpeedUp()) { throw new InvalidOperationException("この路線はスピードアップできません"); }

			//お金チェックと消費
			long cost = CalcSpeedUpCost(gameInfo);
			SpendMoney(gameInfo, cost);

			this.bestSpeedUpKaisu++;
			this.bestSpeed = GetSpeedUppedBestSpeed();
		}

		/// <summary>
		/// 非電化可能か
		/// </summary>
		/// <returns></returns>
		public bool CanUnElectrify()
		{
			if (!IsExist) { return false; }
			if (IsElectrified == true && Type == RailTypeEnum.Iron)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// 非電化工事
		/// </summary>
		/// <param name="gameInfo"></param>
		public void UnElectrify(GameInfo gameInfo)
		{
			if (!CanUnElectrify()) { throw new InvalidOperationException("この路線は非電化できません"); }
			IsElectrified = false;
			DiagramReset();
		}

		/// <summary>
		/// 電化可能か
		/// </summary>
		/// <returns></returns>
		public bool CanElectrify()
		{
			if (!IsExist) { return false; }
			if (IsElectrified == false && Type == RailTypeEnum.Iron)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// 電化コスト計算
		/// </summary>
		/// <param name="gameInfo"></param>
		/// <returns></returns>
		public long CalcElectrifyCost(GameInfo gameInfo)
		{
			return CalcConstructCost(bestSpeed,
									LaneNum,
									Type,
									gauge,
									IsElectrified,
									taihisen,
									gameInfo) * 2 / 3;
		}

		/// <summary>
		/// 電化
		/// </summary>
		/// <param name="gameInfo"></param>
		public void Electrify(GameInfo gameInfo)
		{
			if (!CanElectrify()) { throw new InvalidOperationException("この路線は電化できません"); }

			long cost = CalcElectrifyCost(gameInfo);
			SpendMoney(gameInfo, cost);

			IsElectrified = true;
			DiagramReset();
		}

		/// <summary>
		/// 狭軌に変更可能か
		/// </summary>
		/// <returns></returns>
		public bool CanNarrowGauge()
		{
			if (!IsExist) { return false; }
			if (Type == RailTypeEnum.Iron && gauge == RailGaugeEnum.Regular)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// 軌道縮小コスト計算
		/// </summary>
		/// <param name="gameInfo"></param>
		/// <returns></returns>
		public long CalcNarrowGaugeCost(GameInfo gameInfo)
		{
			return CalcConstructCost(bestSpeed,
									LaneNum,
									Type,
									gauge,
									IsElectrified,
									taihisen,
									gameInfo) / 10;
		}

		/// <summary>
		/// 軌道縮小
		/// </summary>
		/// <param name="gameInfo"></param>
		public void NarrowGauge(GameInfo gameInfo)
		{
			if (!CanNarrowGauge()) { throw new InvalidOperationException("この路線は狭軌に変更できません"); }

			long cost = CalcNarrowGaugeCost(gameInfo);
			SpendMoney(gameInfo, cost);

			gauge = RailGaugeEnum.Narrow;
			DiagramReset();
		}

		/// <summary>
		/// 標準軌に変更可能か
		/// </summary>
		/// <returns></returns>
		public bool CanExpanseGauge()
		{
			if (!IsExist) { return false; }
			if (Type == RailTypeEnum.Iron && gauge == RailGaugeEnum.Narrow)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// 標準軌に変更するコストを計算
		/// </summary>
		/// <param name="gameInfo"></param>
		/// <returns></returns>
		public long CalcExpanseGaugeCost(GameInfo gameInfo)
		{
			return CalcConstructCost(bestSpeed,
									LaneNum,
									Type,
									gauge,
									IsElectrified,
									taihisen,
									gameInfo) / 3;
		}

		/// <summary>
		/// 標準軌に変更
		/// </summary>
		/// <param name="gameInfo"></param>
		public void ExpanseGauge(GameInfo gameInfo)
		{
			if (!CanExpanseGauge()) { throw new InvalidOperationException("この路線は標準軌に変更できません"); }

			long cost = CalcExpanseGaugeCost(gameInfo);
			SpendMoney(gameInfo, cost);

			gauge = RailGaugeEnum.Regular;
			DiagramReset();
		}

		/// <summary>
		/// 路線増設可能か
		/// </summary>
		/// <returns></returns>
		public bool CanAddLane()
		{
			if (!IsExist) { return false; }
			return true;
		}

		/// <summary>
		/// 路線増設コスト計算
		/// </summary>
		/// <param name="gameInfo"></param>
		/// <returns></returns>
		public long CalcAddLaneCost(GameInfo gameInfo)
		{

			return CalcConstructCost(bestSpeed,
									AddedLaneNum(),
									Type,
									gauge,
									IsElectrified,
									TaihisenEnum.None,
									gameInfo);
		}

		/// <summary>
		/// 増設レーン数
		/// </summary>
		/// <returns></returns>
		private int AddedLaneNum()
		{
			if (LaneNum == 1) { return 1; }
			else if (LaneNum > 1) { return 2; }

			throw new InvalidOperationException("レーン数が異常です");
		}

		/// <summary>
		/// 路線増設
		/// </summary>
		/// <param name="gameInfo"></param>
		public void AddLane(GameInfo gameInfo)
		{
			if (!CanAddLane()) { throw new InvalidOperationException("この路線は増設できません"); }

			long cost = CalcAddLaneCost(gameInfo);
			SpendMoney(gameInfo, cost);

			LaneNum += AddedLaneNum();
			taihisen = TaihisenEnum.None;
			if (LaneNum >= 4)
			{
				diagram = DiagramType.Parallel;
			}
			else
			{
				diagram = DiagramType.LimittedExpressPrior;
			}
		}

		/// <summary>
		/// 路線削減/廃止可能か
		/// </summary>
		/// <returns></returns>
		public bool CanReduceOrRemoveLane()
		{
			return IsExist;
		}

		/// <summary>
		/// 削減か廃止か
		/// </summary>
		/// <returns>true:削減  false:廃止</returns>
		public bool IsReduceOrRemoveLane()
		{
			return LaneNum > 1;
		}

		/// <summary>
		/// 路線削減/廃止
		/// </summary>
		/// <param name="gameInfo"></param>
		public void ReduceOrRemoveLane(GameInfo gameInfo)
		{
			if (!CanReduceOrRemoveLane()) { throw new InvalidOperationException("この路線は削減できません"); }

			if (IsReduceOrRemoveLane())
			{
				//削減
				if (LaneNum <= 2)
				{
					LaneNum--;
				}
				else
				{
					LaneNum -= 2;
				}
			}
			else
			{
				//廃止と初期化
				bestSpeed = 0;
				LaneNum = 0;
				bestSpeedUpKaisu = 0;
				diagram = DiagramType.LimittedExpressPrior;
				IsElectrified = null;
				taihisen = TaihisenEnum.Every20km;

				IsExist = false;
			}
			DiagramReset();
		}

		/// <summary>
		/// 待避線を変更できるか
		/// </summary>
		/// <returns></returns>
		public bool CanTaihiChange()
		{
			if (!IsExist) { return false; }
			return LaneNum < 4;
		}

		/// <summary>
		/// 待避線変更コスト計算
		/// </summary>
		/// <param name="taihisen">変更後の待避線間隔</param>
		/// <param name="gameInfo">ゲーム情報</param>
		/// <returns></returns>
		public long CalcTaihisenChangeCost(TaihisenEnum taihisen, GameInfo gameInfo)
		{
			var cost = CalcConstructCost(bestSpeed,
									LaneNum,
									Type,
									gauge,
									IsElectrified,
									taihisen,
									gameInfo) * 11 / 10
									-
									CalcConstructCost(bestSpeed,
									LaneNum,
									Type,
									gauge,
									IsElectrified,
									this.taihisen,
									gameInfo);
			return cost < 0 ? 0 : cost;
		}

		/// <summary>
		/// 待避線間隔を変更
		/// </summary>
		public void ChangeTaihi(TaihisenEnum taihisen, GameInfo gameInfo)
		{
			if (!CanTaihiChange()) { throw new InvalidOperationException("この路線は待避線を設定できません"); }

			var cost = CalcTaihisenChangeCost(taihisen, gameInfo);
			SpendMoney(gameInfo, cost);

			this.taihisen = taihisen;
		}

		/// <summary>
		/// 路線が編成を設定可能な状態か
		/// </summary>
		/// <returns></returns>
		public bool CanCompositionSetting() => IsExist;

		/// <summary>
		/// 路線が編成を受け入れ可能か検証
		/// </summary>
		public void ValidateCompositionAcceptable(IComposition composition, GameInfo gameInfo)
		{
			if (composition is null)
			{
				throw new ArgumentNullException(nameof(composition));
			}

			switch (Type)
			{
				case RailTypeEnum.LinearMotor:
					if (composition.Type != RailTypeEnum.LinearMotor) { throw new CompositionNotAppliedException("車両がリニアではありません"); }
					break;
				case RailTypeEnum.Iron:
					if (composition.Type == RailTypeEnum.LinearMotor) { throw new CompositionNotAppliedException("リニア軌道ではありません"); }
					if (composition.Gauge == CarGaugeEnum.Narrow && gauge == RailGaugeEnum.Regular) { throw new CompositionNotAppliedException("路線幅が違います"); }
					if (composition.Gauge == CarGaugeEnum.Regular && gauge == RailGaugeEnum.Narrow) { throw new CompositionNotAppliedException("路線幅が違います"); }
					if (composition.IsElectrified && !(bool)IsElectrified) { throw new CompositionNotAppliedException("路線が非電化です"); }
					if (composition.Power == PowerEnum.Steam && !gameInfo.IsSteamAvailable()) { throw new CompositionNotAppliedException("蒸気機関車は時代遅れで使えません"); }
					break;
				default:
					throw new InvalidOperationException("未定義の軌道タイプです。検査を追加してください");
			}
		}

		/// <summary>
		/// 不足している編成数を計算
		/// </summary>
		/// <param name="newComposition"></param>
		/// <param name="runningPerDay"></param>
		/// <param name="newDiagramType"></param>
		/// <returns></returns>
		public int CalcMissingCompositions(IComposition newComposition, int runningPerDay, DiagramType newDiagramType)
		{
			int newUseCompNum = CalcUseCompositionNum(newComposition, runningPerDay, newDiagramType);

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
		/// 路線に編成を設定
		/// </summary>
		/// <param name="newComposition">設定する編成</param>
		/// <param name="runningPerDay">日間運行本数</param>
		/// <param name="gameInfo">ゲーム情報</param>
		public void SettingComposition(IComposition newComposition, int runningPerDay, DiagramType newDiagramType, GameInfo gameInfo)
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
			if (!CanCompositionSetting()) { throw new InvalidOperationException("この路線には編成が設定できません"); }
			ValidateCompositionAcceptable(newComposition, gameInfo);

			int newUseCompositionNum;
			if (newDiagramType == DiagramType.None) { newUseCompositionNum = 0; }
			else
			{
				newUseCompositionNum = CalcUseCompositionNum(newComposition, runningPerDay, newDiagramType);
			}

			if (newComposition.HeldUnits < CalcMissingCompositions(newComposition, runningPerDay, newDiagramType))
			{
				throw new InvalidOperationException("編成が足りません");
			}

			//既存の編成を解放→新を確保して設定
			ReleaseComposition();
			newComposition.Use(newUseCompositionNum);
			this.runningPerDay = runningPerDay;
			useCompositionNum = newUseCompositionNum;
			useComposition = newComposition;
			diagram = newDiagramType;
		}

		/// <summary>
		/// 所要時間
		/// </summary>
		/// <param name="composition">計算に用いる編成</param>
		/// <param name="diagramType">ダイアグラム型</param>
		/// <returns>何分掛かるか</returns>
		public int CalcRequieredMinutes(IComposition composition, DiagramType diagramType)
		{
			int ave = CalcAverageSpeed(composition, diagramType);
			if (ave == 0) { return int.MaxValue; }
			int minutes = Distance * 60 / ave;
			return minutes != 0 ? minutes : 1;
		}

		/// <summary>
		/// 所要時間(既存ダイヤグラムで計算)
		/// </summary>
		/// <param name="composition">編成</param>
		/// <returns>何分掛かるか</returns>
		public int CalcRequieredMinutes(IComposition composition) => CalcRequieredMinutes(composition, diagram);

		/// <summary>
		/// 平均速度
		/// </summary>
		/// <param name="composition">計算に用いる編成</param>
		/// <param name="diagramType">ダイアグラム型</param>
		/// <returns>平均速度 km/h</returns>
		public int CalcAverageSpeed(IComposition composition, DiagramType diagramType)
		{
			int bestSpeed = CalcBestSpeed(composition);
			if (LaneNum >= 4) { return bestSpeed * 9 / 10; }
			return diagramType switch
			{
				DiagramType.Regular => bestSpeed * 3 / 4,
				DiagramType.LimittedExpressPrior => bestSpeed * 9 / 10,
				DiagramType.OverCrowded => bestSpeed * 2 / 3,
				DiagramType.Parallel => bestSpeed / 2,
				DiagramType.None => throw new ArgumentException("ダイヤなしでは平均速度を算出できません"),
				_ => throw new InvalidOperationException("平均速度を取得できないケースです"),
			};
		}

		/// <summary>
		/// 最大速度
		/// </summary>
		/// <param name="composition">編成</param>
		/// <returns>最大速度</returns>
		public int CalcBestSpeed(IComposition composition)
		{
			if (composition is null)
			{
				Console.Error.WriteLine("編成が指定されていませんので最高速度0となります");
				return 0;
			}
			//路線のほうが速いなら編成側が上限になる
			if (bestSpeed >= composition.BestSpeed) { return composition.BestSpeed; }

			//編成＞路線のとき、編成を振り子で高速化した範囲を超えない限りで編成側が速い
			switch (composition.Tilt)
			{
				case CarTiltEnum.Pendulum:
				case CarTiltEnum.SimpleMecha:
					return Math.Min(bestSpeed * 6 / 5, composition.BestSpeed);
				case CarTiltEnum.HighMecha:
					return Math.Min(bestSpeed * 4 / 3, composition.BestSpeed);
				case CarTiltEnum.None:
					return bestSpeed;
				default:
					throw new InvalidOperationException("最大速度を取得できないケースです");
			}
		}

		/// <summary>
		/// 投入編成数を算出
		/// </summary>
		/// <param name="composition"></param>
		/// <param name="runningPerDay"></param>
		/// <param name="diagramType">見積もりに使うダイアグラム型</param>
		/// <returns></returns>
		public int CalcUseCompositionNum(IComposition composition, int runningPerDay, DiagramType diagramType)
		{
			return (int)MathF.Ceiling((float)runningPerDay * CalcRequieredMinutes(composition, diagramType) / 540);
		}

		/// <summary>
		/// 路線の運行本数を変更したときの乗車率を予測
		/// </summary>
		/// <returns></returns>
		public int PredicateJosharitsu(IComposition composition, int runningPerDay)
		{
			if (!IsExist) { return 0; }

			int keitoDiagramCapacity = belongingKeitoDiagrams.Select(keito => keito.PassengerCapacityPerDay).Sum();
			int baseNum = keitoDiagramCapacity + LogicUtil.CalcMaximumCapacity(composition, runningPerDay);
			return CalcJosharitsuByCapacity(baseNum);
		}

		/// <summary>
		/// 系統ダイアグラムと路線ダイアのキャパシティから乗車率を計算
		/// </summary>
		/// <param name="capacity">乗車可能数</param>
		/// <returns></returns>
		private int CalcJosharitsuByCapacity(int capacity)
		{
			if (capacity == 0) { return 0; }

			if (passengersLastWeek <= 1000000) { return passengersLastWeek * 100 * 50 / 7 / capacity; }
			else { return passengersLastWeek * 71 * 10 / capacity; }
		}

		/// <summary>
		/// 適したダイアグラムを判定
		/// </summary>
		/// <param name="runningPerDay"></param>
		/// <param name="genkaiKyoyo"></param>
		/// <returns></returns>
		public DiagramType JudgeSuitableDiagram(int runningPerDay, int genkaiKyoyo)
		{
			//特急→普通→過密→並行とキャパの少ない順に見ていって、運行できるようになったらそのダイヤに決定

			DiagramType diagram = DiagramType.LimittedExpressPrior;
			if (CalcExcessCapacity(diagram, runningPerDay, true, genkaiKyoyo) >= 0) { return diagram; }

			diagram = DiagramType.Regular;
			if (CalcExcessCapacity(diagram, runningPerDay, true, genkaiKyoyo) >= 0) { return diagram; }

			diagram = DiagramType.OverCrowded;
			if (CalcExcessCapacity(diagram, runningPerDay, true, genkaiKyoyo) >= 0) { return diagram; }

			diagram = DiagramType.Parallel;
			if (CalcExcessCapacity(diagram, runningPerDay, true, genkaiKyoyo) >= 0) { return diagram; }

			//適切なダイヤなし
			return DiagramType.None;
		}

		/// <summary>
		/// 指定されたダイアで路線の余剰本数計算
		/// </summary>
		/// <param name="diagram">ダイアグラムタイプ</param>
		/// <param name="runningPerDay">日毎運行本数</param>
		/// <param name="isIncludeKamotsu">貨物を含めるか</param>
		/// <param name="genkaiKyoyo">追加する許容路線本数</param>
		/// <returns></returns>
		private int CalcExcessCapacity(DiagramType diagram, int runningPerDay, bool isIncludeKamotsu, int genkaiKyoyo)
		{
			int genkai = CalcGenkaiHonsuu(diagram, genkaiKyoyo);

			int total = runningPerDay + TripsOfBelongingDiagrams + (isIncludeKamotsu ? kamotsuNumLastWeek : 0);

			return genkai - total;
		}

		/// <summary>
		/// 現在の余剰本数 hno
		/// </summary>
		/// <param name="IsIncludeKamotsu"></param>
		/// <param name="genkaiKyoyo"></param>
		/// <returns></returns>
		public int CalcExcessCapacity(bool IsIncludeKamotsu, int genkaiKyoyo) =>
			CalcExcessCapacity(diagram, runningPerDay, IsIncludeKamotsu, genkaiKyoyo);

		/// <summary>
		/// 指定された系統を除く路線の余剰本数計算(路線ダイアグラム指定)
		/// </summary>
		/// <param name="keito">系統ダイヤの合計走行本数から除く系統ダイヤ</param>
		/// <param name="isIncludeKamotsu">貨物を含めるか</param>
		/// <param name="genkaiKyoyo">追加する許容路線本数</param>
		/// <returns></returns>
		public int CalcExcessCapacityWithoutSpecifiedKeito(KeitoDiagram keito, bool isIncludeKamotsu, int genkaiKyoyo)
		{
			int genkai = CalcGenkaiHonsuu(diagram, genkaiKyoyo);

			int total = runningPerDay + (TripsOfBelongingDiagrams - keito.runningPerDay) + (isIncludeKamotsu ? kamotsuNumLastWeek : 0);

			return genkai - total;
		}

		/// <summary>
		/// 路線の平均所要時間を算出 sta
		/// </summary>
		/// <returns></returns>
		public int CalcAverageRequireMinutes()
		{
			int hyokaSpeed = CalcHyokaSpeed();
			if (hyokaSpeed == 0) { return 0; }
			hyokaSpeed = Distance * 60 / hyokaSpeed;
			return hyokaSpeed == 0 ? 1 : hyokaSpeed;
		}

		/// <summary>
		/// 評価速度を算出 ssp
		/// </summary>
		/// <returns></returns>
		private int CalcHyokaSpeed()
		{
			//路線と系統の速度の平均=評価速度

			int lineAverageSpeed = 0;
			if (useCompositionNum > 0) { lineAverageSpeed = CalcAverageSpeed(useComposition, diagram); }
			int[] keitoAveSpeeds = belongingKeitoDiagrams.Select(keito =>
			{
				//各系統のこの路線にあたる部分だけの平均速度を求める
				Line line = keito.route.Where(lineInKeito => lineInKeito == this).First();
				return line.CalcAverageSpeed(keito.useComposition, line.diagram);
			}).ToArray();
			return (lineAverageSpeed + keitoAveSpeeds.Sum()) / (1 + keitoAveSpeeds.Length);
		}

		/// <summary>
		/// 路線で最も乗り心地の悪い座席を取得 sca
		/// </summary>
		/// <returns></returns>
		public SeatEnum? WorstComfortLevelSeat()
		{
			List<IComposition> useCompositionList = new List<IComposition>();

			if (useComposition != null)
			{
				useCompositionList.Add(useComposition);
			}
			List<IComposition> keitoComps =
				belongingKeitoDiagrams.Where(keito => keito.useComposition != null).Select(keito => keito.useComposition).ToList();
			useCompositionList.AddRange(keitoComps);

			return useCompositionList.OrderBy(composition => composition.BestComfortSeat?.ToComfortLevel() ?? int.MaxValue).FirstOrDefault()?.BestComfortSeat;
		}

		/// <summary>
		/// 各路線乗客数計算
		/// </summary>
		/// <param name="isPopulationConsidered">沿線人口考慮するか</param>
		/// <returns></returns>
		public int CalcPassengersNumOnlyLine(bool isPopulationConsidered, GameInfo gameInfo)
		{
			if (IsExist == false || TotalNumberTrips(false) == 0) { return 0; }

			// often 運行頻度スコア 3
			int passengers = (TotalNumberTrips(false) * (gameInfo.Year - (gameInfo.BasicYear - 180)))
					+ (100 * (gameInfo.BasicYear + 120 - gameInfo.Year));
			if (passengers > 30000) { passengers = 30000; }
			else if (passengers < 0) { passengers = 0; }

			// speed 速達スコア 5
			int requireMinutes = Distance * 60 / CalcHyokaSpeed();
			if (requireMinutes < 10) { passengers += 30000; }
			if ((requireMinutes >= 10) && (requireMinutes < 310)) { passengers += 30000 - ((requireMinutes - 10) * 100); }
			if (requireMinutes < 60) { passengers += 21000 - ((requireMinutes - 10) * 300); }
			if (passengers < 0) { passengers = 0; }

			// Car 車両快適性スコア 1.5
			passengers += WorstComfortLevelSeat()?.ToPassengerNumBonus() ?? 0;
			passengers += useComposition?.Power == PowerEnum.Electricity || useComposition?.Power == PowerEnum.LinearMotor ? 5000 : 0; //電車とリニアはボーナス

			if (!isPopulationConsidered)
			{
				return passengers;
			}

			passengers = passengers / 100 * retentionRate / 10000 * LinePopulation / 2500 * gameInfo.Rpm / 100;

			switch (propertyType)
			{
				case LinePropertyType.Surburb:
					passengers *= 5;
					break;
				case LinePropertyType.Outskirts:
					passengers *= 8;
					break;
				case LinePropertyType.Underground:
					passengers *= 10;
					break;
			}
			return passengers * gameInfo.CalcEconomicIndex() / 100;
		}

		/// <summary>
		/// 路線人口
		/// </summary>
		/// <returns></returns>
		public int LinePopulation => Start.Population * End.Population / 10;

		/// <summary>
		/// 定着率調整
		/// </summary>
		/// <returns></returns>
		public void AdjustRetentionRate()
		{
			if (passengersLastWeek == 0) { return; }

			retentionRate++;
			if (Josharitsu <= 80) { retentionRate++; }
			//乗車率が高いほど定着率を下げる
			if (Josharitsu > 120) { retentionRate--; }
			if (Josharitsu > 140) { retentionRate--; }
			if (Josharitsu > 160) { retentionRate--; }
			if (Josharitsu > 180) { retentionRate--; }
			if (Josharitsu >= 200)
			{
				//乗車率200%超えなら200%に乗客を修正
				passengersLastWeek = passengersLastWeek * 200 / Josharitsu;
				retentionRate--;
			}
			retentionRate = Math.Min(30000, retentionRate);
		}

		/// <summary>
		/// 収入を計算して反映
		/// </summary>
		public void CalcAndReflectIncome(GameInfo gameInfo)
		{
			if (IsExist == false || TotalNumberTrips(false) == 0)
			{
				incomeLastWeek = 0;
				return;
			}

			if (passengersLastWeek < 0) { passengersLastWeek = 0; }
			if (passengersLastWeek == 0)
			{
				incomeLastWeek = 0;
				return;
			}

			if (Josharitsu > 200)
			{
				int keitoCapacity = belongingKeitoDiagrams.Select(keito => keito.PassengerCapacityPerDay).Sum();
				int baseNum = keitoCapacity + PassengerCapacityPerDay;

				passengersLastWeek = baseNum * 2;
			}

			incomeLastWeek = passengersLastWeek / 20 * CalcFare(gameInfo) / 100;

		}

		/// <summary>
		/// 旅客運賃を計算 uhs
		/// </summary>
		/// <returns></returns>
		private int CalcFare(GameInfo gameInfo)
		{
			if (propertyType == LinePropertyType.Outskirts || propertyType == LinePropertyType.Underground)
			{
				if (gameInfo.Year < (gameInfo.BasicYear + 120)) { return Distance * gameInfo.FarePerKm * 200 / (gameInfo.Year - gameInfo.BasicYear + 80) + 10; }
				else { return Distance * gameInfo.FarePerKm + 10; }
			}

			//リニア料金
			if (CalcHyokaSpeed() > ((gameInfo.Year - gameInfo.BasicYear) * 2 + 260)) { return (Distance * gameInfo.FarePerKm + 50) * 3 / 2; }

			//新幹線料金
			if (CalcHyokaSpeed() > ((gameInfo.Year - gameInfo.BasicYear) + 120)) { return (Distance * gameInfo.FarePerKm + 50) * 6 / 5; }

			//特急料金
			if (CalcHyokaSpeed() > ((gameInfo.Year - gameInfo.BasicYear) * 2 + 40)) { return (Distance * gameInfo.FarePerKm + 50) * 11 / 10; }

			return Distance * gameInfo.FarePerKm + 50;
		}

		/// <summary>
		/// 貨物運賃を計算
		/// </summary>
		/// <param name="kamotsuTanka"></param>
		/// <returns></returns>
		public int CalcKamotsuFare(int kamotsuTanka) => kamotsuTanka * 7 * kamotsuNumLastWeek * Distance / 1500;

		/// <summary>
		/// 支出計算
		/// </summary>
		/// <param name="gameInfo"></param>
		/// <returns></returns>
		public int CalcOutcome(GameInfo gameInfo)
		{
			if (IsExist == false) { return 0; }
			int result = 0;
			if (useCompositionNum > 5)
			{
				int opCost = useComposition.CalcOperatingCost(gameInfo.Year, gameInfo.BasicYear);
				result = opCost * useCompositionNum;
			}
			result += (int)CalcConstructCost(bestSpeed, LaneNum, Type, gauge, IsElectrified, taihisen, gameInfo) / 1000;
			result += gameInfo.isDevelopedAutoGate ? passengersLastWeek / 10000 : passengersLastWeek / 1000;

			foreach (var keito in belongingKeitoDiagrams)
			{
				if (keito.useCompositionNum == 0) { continue; }

				int routeAllDistance = keito.route.Sum(line => line.Distance);
				int keitoOpCost = keito.useComposition.CalcOperatingCost(gameInfo.Year, gameInfo.BasicYear);
				result += keitoOpCost * keito.useCompositionNum * Distance / routeAllDistance;
			}

			if (TotalNumberTrips(false) == 0) { result /= 2; }
			return result;
		}
	}
}