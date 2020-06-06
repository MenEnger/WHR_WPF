using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using whr_wpf.Util;

namespace whr_wpf.Model
{
	/// <summary>
	/// ゲーム状態保持クラス
	/// </summary>
	[Serializable()]
	public partial class GameInfo : INotifyPropertyChanged
	{
		protected void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		public event PropertyChangedEventHandler PropertyChanged;

		public GameInfo()
		{
		}

		/// <summary>
		/// 難易度
		/// </summary>
		public DifficultyLevelEnum? Difficulty { get; set; } = null;

		/// <summary>
		/// 選択されたモード set時にgameInfoの設定を初期化していく
		/// </summary>
		public Mode SelectedMode
		{
			get => _selectedMode; set
			{
				_selectedMode = value;
				ApplyModeSetting(value);
			}
		}

		/// <summary>
		/// モードごとの設定を適用
		/// </summary>
		/// <param name="value"></param>
		private void ApplyModeSetting(Mode value)
		{
			Year = value.Year;
			Money = value.Money;
			MYear = value.MYear;

			//TODO ここにゲーム設定、技術開発、技術開発状況に応じた限界許容本数の追加、路線、運行系統のダイヤ等、目標読み込み時の処理を追記

			//技術開発
			genkaiJoki = Math.Min(value.genkaiJoki, 150);
			genkaiDenki = value.genkaiDenki ?? genkaiDenki;
			genkaiKidosha = value.genkaiKidosha ?? genkaiKidosha;
			genkaiLinear = value.genkaiLinear ?? genkaiLinear;
			if (value.isDevelopedDynamicSignal)
			{
				isDevelopedDynamicSignal = true;
				genkaikyoyo += 5;
			}
			if (value.isDevelopedFreeGauge || isDevelopedDynamicSignal)
			{
				isDevelopedFreeGauge = true;
			}
			if (value.isDevelopedMachineTilt || isDevelopedFreeGauge)
			{
				isDevelopedMachineTilt = true;
			}
			if (value.isDevelopedDualSeat || isDevelopedMachineTilt)
			{
				isDevelopedDualSeat = true;
			}
			if (value.isDevelopedRetructableLong || isDevelopedDualSeat)
			{
				isDevelopedRetructableLong = true;
			}
			if (value.isDevelopedRichCross || isDevelopedRetructableLong)
			{
				isDevelopedRichCross = true;
			}
			if (value.isDevelopedCarTiltPendulum || isDevelopedRichCross)
			{
				isDevelopedCarTiltPendulum = true;
			}
			if (value.isDevelopedAutoGate || isDevelopedCarTiltPendulum)
			{
				isDevelopedAutoGate = true;
			}
			if (value.isDevelopedConvertibleCross || isDevelopedAutoGate)
			{
				isDevelopedConvertibleCross = true;
			}
			if (value.isDevelopedBlockingSignal || isDevelopedConvertibleCross)
			{
				isDevelopedBlockingSignal = true;
				genkaikyoyo += 5;
			}

			//累計投資額調整
			//TODO 動力の技術開発の調整も実装する
			AccumulatedInvest.steam = (long)Math.Pow(genkaiJoki - 35, 3) / 5 * TechCost / 4;
			AccumulatedInvest.electricMotor = genkaiDenki <= 360
				? (long)Math.Pow(genkaiDenki + 5, 3) / 10 * TechCost / 2
				: ((50000 * (genkaiDenki - 10)) + 30377125) / 10 * TechCost / 2;
			if (genkaiDenki < 60) { genkaiDenki = 0; }
			if (genkaiDenki > 0 && genkaiJoki < 100) { genkaiJoki = 100; }
			//todo ディーゼルとリニア
			
			if (isDevelopedDynamicSignal) { AccumulatedInvest.newPlan = 500000 * TechCost / 20; }
			else if (isDevelopedFreeGauge) { AccumulatedInvest.newPlan = 300000 * TechCost / 20; }
			else if (isDevelopedMachineTilt) { AccumulatedInvest.newPlan = 200000 * TechCost / 20; }
			else if (isDevelopedDualSeat) { AccumulatedInvest.newPlan = 100000 * TechCost / 20; }
			else if (isDevelopedRetructableLong) { AccumulatedInvest.newPlan = 50000 * TechCost / 20; }
			else if (isDevelopedRichCross) { AccumulatedInvest.newPlan = 30000 * TechCost / 20; }
			else if (isDevelopedCarTiltPendulum) { AccumulatedInvest.newPlan = 20000 * TechCost / 20; }
			else if (isDevelopedAutoGate) { AccumulatedInvest.newPlan = 10000 * TechCost / 20; }
			else if (isDevelopedConvertibleCross) { AccumulatedInvest.newPlan = 5000 * TechCost / 20; }
			else if (isDevelopedBlockingSignal) { AccumulatedInvest.newPlan = 1000 * TechCost / 20; }

			//路線のデフォ設定
			lines.Zip(value.LineSettings, (line, setting) => new { Line = line, Setting = setting }).ToList().ForEach(it =>
				{
					it.Line.IsExist = it.Setting.IsExist;
					it.Line.Type = it.Setting.Type;
					it.Line.gauge = it.Setting.gauge;
					it.Line.IsElectrified = it.Setting.IsElectrified;
					it.Line.bestSpeed = it.Setting.bestSpeed;
					it.Line.LaneNum = it.Setting.LaneNum;
					it.Line.retentionRate = it.Setting.retentionRate;
					it.Line.taihisen = it.Setting.taihisen;
					it.Line.useComposition = it.Setting.useComposition;
					it.Line.runningPerDay = it.Setting.runningPerDay;
				});

			//運行系統のデフォルト運行設定
			diagrams.Zip(value.KeitoDefaultSettings, (keito, setting) =>
			(Keito: keito, Setting: setting)).ToList().ForEach(it =>
				{
					it.Keito.useComposition = it.Setting.useComposition;
					it.Keito.runningPerDay = it.Setting.runningPerDay;
				});

			//各路線ダイヤ設定、系統と路線の投入編成数の決定
			lines.ForEach(line =>
			{
				int totalRunningPerDay = line.TotalNumberTrips(false);

				int genkai = line.CalcGenkaiHonsuu(DiagramType.LimittedExpressPrior, genkaikyoyo);
				if (totalRunningPerDay <= genkai)
				{
					line.diagram = DiagramType.LimittedExpressPrior;
					return;
				}

				genkai = line.CalcGenkaiHonsuu(DiagramType.Regular, genkaikyoyo);
				if (totalRunningPerDay <= genkai)
				{
					line.diagram = DiagramType.Regular;
					return;
				}

				genkai = line.CalcGenkaiHonsuu(DiagramType.OverCrowded, genkaikyoyo);
				if (totalRunningPerDay <= genkai)
				{
					line.diagram = DiagramType.OverCrowded;
					return;
				}

				genkai = line.CalcGenkaiHonsuu(DiagramType.Parallel, genkaikyoyo);
				if (totalRunningPerDay <= genkai)
				{
					line.diagram = DiagramType.Parallel;
					return;
				}

				throw new CannotContinueException($"シナリオダイヤ設定エラーです。路線'{line.Caption}'のスペックが路線と系統に設定された運行本数を捌けません。");
			});
			diagrams.ForEach(keito =>
			{
				ImmutableDictionary<Line, DiagramType> lineDiagrams = keito.route.ToImmutableDictionary(
					line => line,
					line => line.diagram);
				keito.useCompositionNum = keito.CalcUseCompositionNum(keito.useComposition, keito.runningPerDay, lineDiagrams);
			});
			lines.ForEach(line =>
			{
				line.useCompositionNum = line.CalcUseCompositionNum(line.useComposition, line.runningPerDay, line.diagram);
			});

			compositions.AddRange(value.DefautltCompositions);
		}

		private Mode _selectedMode = null;

		/// <summary>
		/// シナリオバージョン
		/// </summary>
		public int ScenerioVersion { get; set; }

		/// <summary>
		/// 基礎とする年
		/// </summary>
		public int BasicYear { get; set; }

		/// <summary>
		/// 乗客数動態
		/// </summary>
		public SeasonEnum Season { get; set; }

		/// <summary>
		/// 蒸気機関車 使用制限開始年 styear
		/// </summary>
		public int SteamYear { get; set; }

		/// <summary>
		/// 貨物動態
		/// </summary>
		public KamotsuEnum Kamotu { get; set; }

		/// <summary>
		/// kmあたりの運賃 km
		/// </summary>
		public int FarePerKm { get; set; }

		/// <summary>
		/// 乗車数　基準を100とする乗車数の倍率 rpm
		/// </summary>
		public int Rpm { get; set; }

		/// <summary>
		/// 路線作成費　基準を100とする路線作成費の倍率。維持費にも影響 linemc
		/// </summary>
		public int LineMakeCost { get; set; }

		/// <summary>
		/// 技術開発費　基準を100とする技術開発費の倍率 tecc
		/// </summary>
		public int TechCost { get; set; }

		/// <summary>
		/// 人口上昇倍率 uppeo
		/// </summary>
		public int UpperPopulation { get; set; }

		/// <summary>
		/// 上限人口 maxpp
		/// </summary>
		public int MaxPopulation { get; set; }

		/// <summary>
		/// マップ数 最初は使わない
		/// </summary>
		public int Maps { get; set; }

		/// <summary>
		/// 情報表示位置
		/// </summary>
		public InfoPosiEnum InfoPosi { get; set; }

		/// <summary>
		/// 政府補助金 開始年 hojo
		/// </summary>
		public int HojoStartYear { get; set; }

		/// <summary>
		/// 政府補助金 終了年 hojo
		/// </summary>
		public int HojoEndYear { get; set; }

		/// <summary>
		/// 政府補助金 金額(10万単位) hojo
		/// </summary>
		public int HojoAmount { get; set; }

		/// <summary>
		/// 戦時モード定義
		/// </summary>
		public List<WarMode> warModeList;

		/// <summary>
		/// 所持金 10万単位
		/// </summary>
		public long Money
		{
			get => money; set
			{
				money = value;
				OnPropertyChanged(nameof(Money));
			}
		}
		private long money = 5000000;

		/// <summary>
		/// 年
		/// </summary>
		public int Year { get; set; } = 1880;

		/// <summary>
		/// 月
		/// </summary>
		public int Month { get; set; } = 1;

		/// <summary>
		/// 週
		/// </summary>
		public int Week { get; set; } = 1;

		/// <summary>
		/// ゲームオーバー年
		/// </summary>
		public int MYear { get; set; }

		/// <summary>
		/// 総人口
		/// </summary>
		public int Ap { get; set; } = 0;

		/// <summary>
		/// モード
		/// </summary>
		public List<Mode> Modes { get; set; } = new List<Mode>();

		/// <summary>
		/// 編成
		/// </summary>
		public List<IComposition> compositions = new List<IComposition>();

		/// <summary>
		/// 車両
		/// </summary>
		public List<Car> vehicles = new List<Car>();


		/// <summary>
		/// 路線
		/// </summary>
		public List<Line> lines = new List<Line>();

		/// <summary>
		/// 運転系統
		/// </summary>
		public List<KeitoDiagram> diagrams = new List<KeitoDiagram>();

		/// <summary>
		/// 駅
		/// </summary>
		public List<Station> stations = new List<Station>();

		/// <summary>
		/// その週の収入
		/// </summary>
		public int income;

		/// <summary>
		/// その週の支出
		/// </summary>
		public int outlay;

		/// <summary>
		/// 背景の地図
		/// </summary>
		public BitmapImage map;

		/// <summary>
		/// 駅名を表示するか(未実装)
		/// </summary>
		public bool nameShown;

		/// <summary>
		/// 最後に見た運転系統
		/// </summary>
		public KeitoDiagram lastSeenKeito;

		/// <summary>
		/// 最後に見た路線
		/// </summary>
		public Line lastSeenLine;

		/// <summary>
		/// 現在の戦時モード
		/// </summary>
		public WarMode modss = null;

		/// <summary>
		/// 蒸気機関の限界スピード
		/// </summary>
		public int genkaiJoki = 40;

		/// <summary>
		/// 電気モーターの限界スピード
		/// </summary>
		public int genkaiDenki;

		/// <summary>
		/// 気動車の限界スピード
		/// </summary>
		public int genkaiKidosha;

		/// <summary>
		/// リニアの限界スピード
		/// </summary>
		public int genkaiLinear;

		//追加する許容路線本数 gky
		public int genkaikyoyo;

		//新企画技術開発
		public bool isDevelopedCarTiltPendulum; //振り子式車体傾斜装置
		public bool isDevelopedBlockingSignal; //閉塞信号
		public bool isDevelopedFreeGauge; //フリーゲージ
		public bool isDevelopedDualSeat; //デュアルシート
		public bool isDevelopedRichCross; //豪華クロスシート
		public bool isDevelopedRetructableLong; //収納式ロングシート
		public bool isDevelopedConvertibleCross; //転換クロスシート
		public bool isDevelopedAutoGate; //自動改札機
		public bool isDevelopedDynamicSignal; //動的信号
		public bool isDevelopedMachineTilt; //機械式車体傾斜装置

		/// <summary>
		/// 毎週投資額
		/// </summary>
		public InvestmentAmount weeklyInvestment;

		/// <summary>
		/// 累計投資額
		/// </summary>
		public InvestmentAmountAccumulated AccumulatedInvest;

		/// <summary>
		/// 乗り継ぎ
		/// </summary>
		public List<Longway> longwayList;

		private static Random Rnd() => new Random();

		/// <summary>
		/// 経済動向
		/// </summary>
		double[] economyTrends = { Rnd().Next(0, 360), Rnd().Next(0, 360), Rnd().Next(0, 360), Rnd().Next(0, 360) };
		

		/// <summary>
		/// 経済動向指数計算
		/// </summary>
		/// <returns></returns>
		public int CalcEconomicIndex()
		{
			double amplitude = 256 * 3; //上限下限の幅の具合
			double baseIndex = economyTrends
				.Select(angle => Math.Cos(angle * (Math.PI / 180)))
				.Select(cosine => ((cosine * amplitude) + 10000) / 100)
				.Aggregate((kari1, kari2) => kari1 * kari2);
			baseIndex /= 1000000;

			int adjustVal = 0;
			switch (Difficulty)
			{
				case DifficultyLevelEnum.VeryEasy:
					adjustVal = 5;
					break;
				case DifficultyLevelEnum.Easy:
					adjustVal = 2;
					break;
				case DifficultyLevelEnum.Hard:
					adjustVal = -2;
					break;
				case DifficultyLevelEnum.VeryHard:
					adjustVal = -5;
					break;
			}
			return (int)baseIndex + adjustVal;
		}

		/// <summary>
		/// お金を消費
		/// </summary>
		/// <param name="amount">消費資金</param>
		public void SpendMoney(long amount)
		{
			if (Money < amount) { throw new MoneyShortException("お金が足りません"); }
			Money -= amount;
		}

		/// <summary>
		/// 蒸気機関車が新規使用/開発可能か
		/// </summary>
		/// <returns></returns>
		public bool IsSteamAvailable() => Year < SteamYear;

		/// <summary>
		/// 振り子を用いることが可能か
		/// </summary>
		public bool CanUsePendulum() => isDevelopedCarTiltPendulum && !isDevelopedMachineTilt;

		/// <summary>
		/// 車両購入コスト計算
		/// </summary>
		/// <param name="bestSpeed"></param>
		/// <param name="power"></param>
		/// <param name="gauge"></param>
		/// <param name="seat"></param>
		/// <param name="tilt"></param>
		/// <returns></returns>
		public int CalcPurchaseVehicleCost(int bestSpeed, PowerEnum power, CarGaugeEnum gauge, SeatEnum seat, CarTiltEnum tilt) =>
			LogicUtil.CalcPurchaseCarCost(bestSpeed, power, gauge, seat, tilt, genkaiLinear, genkaiJoki, genkaiDenki, genkaiKidosha);

		/// <summary>
		/// 車両開発コスト計算
		/// </summary>
		/// <param name="bestSpeed"></param>
		/// <param name="power"></param>
		/// <param name="gauge"></param>
		/// <param name="seat"></param>
		/// <param name="tilt"></param>
		/// <returns></returns>
		public int CalcDevelopVehicleCost(int bestSpeed, PowerEnum power, CarGaugeEnum gauge, SeatEnum seat, CarTiltEnum tilt) =>
			LogicUtil.CalcPurchaseCarCost(bestSpeed, power, gauge, seat, tilt, genkaiLinear, genkaiJoki, genkaiDenki, genkaiKidosha) * 10;

		/// <summary>
		/// 戦時モード
		/// </summary>
		public class WarMode
		{
			/// <summary>
			/// 戦争開始年
			/// </summary>
			public int StartYear;

			/// <summary>
			/// 戦争終了年
			/// </summary>
			public int EndYear;

			/// <summary>
			/// 貨物増加指数(通常時100)
			/// </summary>
			public int kamotsuIndex;

			/// <summary>
			/// 貨物量に指数を掛ける
			/// </summary>
			/// <param name="kamotsuAmount"></param>
			/// <returns></returns>
			public int MultiplyKamotsu(int kamotsuAmount) => kamotsuAmount * kamotsuIndex / 100;

		}
	}

}
