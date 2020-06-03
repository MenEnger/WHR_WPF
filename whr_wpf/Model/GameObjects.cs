using System;
using System.Collections.Generic;
using System.Linq;

namespace whr_wpf.Model
{
	/// <summary>
	/// index.modのモードの設定を表すクラス
	/// </summary>
	[Serializable()]
	public class Mode
	{
		public Mode() { }

		/// <summary>
		/// モード名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 年
		/// </summary>
		public int Year { get; set; }

		/// <summary>
		/// 所持金 10万円単位
		/// </summary>
		public long Money { get; set; }

		/// <summary>
		/// 開始時のメッセージ
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// ゲームオーバー年
		/// </summary>
		public int MYear { get; set; }

		/// <summary>
		/// 読み込み時の編成
		/// </summary>
		public List<DefautltComposition> DefautltCompositions { get; set; }

		/// <summary>
		/// 路線のデフォルト設定
		/// </summary>
		public List<LineDefaultSetting> LineSettings { get; set; }

		/// <summary>
		/// 運行系統のデフォルト設定
		/// </summary>
		public List<KeitoDefaultSetting> KeitoDefaultSettings { get; set; }

		/// <summary>
		/// 蒸気機関の限界スピード steam
		/// </summary>
		public int genkaiJoki = 40;

		/// <summary>
		/// 電気モーターの限界スピード elect
		/// </summary>
		public int? genkaiDenki;

		/// <summary>
		/// 気動車の限界スピード diesel
		/// </summary>
		public int? genkaiKidosha;

		/// <summary>
		/// リニアの限界スピード linear
		/// </summary>
		public int? genkaiLinear;

		//新企画技術開発 tecno
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
		/// 人口係数の掛ける数 people[0]
		/// </summary>
		public int peopleNume = 1;

		/// <summary>
		/// 人口係数の割る数 people[1]
		/// </summary>
		public int peopleDenom = 1;

		/// <summary>
		/// 路線のデフォルト状態の設定
		/// </summary>
		public class LineDefaultSetting
		{
			/// <summary>
			/// 敷設済みか ltn
			/// </summary>
			public bool IsExist { get; set; }

			/// <summary>
			/// 路線のタイプ lk
			/// </summary>
			public RailTypeEnum Type { get; set; }

			/// <summary>
			/// 軌間幅 lk
			/// </summary>
			public RailGaugeEnum gauge;

			/// <summary>
			/// 電化路線か lk
			/// </summary>
			public bool? IsElectrified { get; set; }

			/// <summary>
			/// 最高速度 lbs
			/// </summary>
			public int bestSpeed;

			/// <summary>
			/// 路線数 las　1=単線　2=複線　4～=複々線～
			/// </summary>
			public int LaneNum { get; set; }

			/// <summary>
			/// 待避線 lts
			/// </summary>
			public TaihisenEnum taihisen = TaihisenEnum.Every20km;

			/// <summary>
			/// 定着率 lwe
			/// </summary>
			public int retentionRate = 10000;  //デフォ10000

			/// <summary>
			/// 投入編成 udc
			/// </summary>
			public DefautltComposition useComposition;

			/// <summary>
			/// 一日あたり運行本数 ulcr
			/// </summary>
			public int runningPerDay;
		}

		/// <summary>
		/// 運行系統のデフォルト状態の設定
		/// </summary>
		public class KeitoDefaultSetting
		{
			/// <summary>
			/// 投入編成 udc
			/// </summary>
			public DefautltComposition useComposition;

			/// <summary>
			/// 日間運行本数 udcr
			/// </summary>
			public int runningPerDay;
		}
	}

	/// <summary>
	/// 編成I/F
	/// </summary>
	public interface IComposition
	{
		/// <summary>
		/// 最高速度
		/// </summary>
		int BestSpeed { get; set; }

		/// <summary>
		/// 編成両数
		/// </summary>
		int CarCount { get; }

		/// <summary>
		/// 車体傾斜装置
		/// </summary>
		public CarTiltEnum Tilt { get; set; }

		/// <summary>
		/// 軌間
		/// </summary>
		CarGaugeEnum? Gauge { get; set; }

		/// <summary>
		/// 電化しているか
		/// </summary>
		bool IsElectrified { get; }

		/// <summary>
		/// 編成名
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// 座席数
		/// </summary>
		int PassengerCapacity { get; }

		/// <summary>
		/// 動力源
		/// </summary>
		PowerEnum Power { get; set; }

		/// <summary>
		/// 編成価格
		/// </summary>
		int Price { get; }

		/// <summary>
		/// 売却価格
		/// </summary>
		int SalePrice => Price / 10;

		/// <summary>
		/// 軌道のタイプ
		/// </summary>
		RailTypeEnum Type { get; set; }

		/// <summary>
		/// 保有編成数(余剰編成数)
		/// </summary>
		public int HeldUnits { get; }

		/// <summary>
		/// 車両の中で最も乗り心地の良い座席 cng
		/// </summary>
		public SeatEnum? BestComfortSeat { get; }

		/// <summary>
		/// 編成購入
		/// </summary>
		/// <param name="gameInfo">ゲーム情報</param>
		/// <param name="quantity">編成購入数</param>
		public void Purchase(GameInfo gameInfo, int quantity);

		/// <summary>
		/// 編成供出
		/// </summary>
		/// <param name="quantity">供出する編成数</param>
		public void Use(int quantity);

		/// <summary>
		/// 編成売却
		/// </summary>
		/// <param name="gameInfo">ゲーム情報</param>
		/// <param name="quantity">売却する数</param>
		public void Sale(GameInfo gameInfo, int quantity)
		{
			if (quantity > HeldUnits) { throw new InvalidOperationException("保有編成数が売却数に対して不足しています"); }
			Use(quantity);
		}

		/// <summary>
		/// 路線/系統ダイヤから編成を解放したときに呼ぶ
		/// </summary>
		/// <param name="useCompositionNum"></param>
		public void Release(int quantity);

		/// <summary>
		/// 車両運行費用の計算
		/// </summary>
		/// <param name="year">今の年</param>
		/// <param name="basicYear">基礎とする年</param>
		/// <returns></returns>
		public int CalcOperatingCost(int year, int basicYear)
		{
			int result = Price * 7 / BestSpeed / 4;
			int personnelFee = year - basicYear + 200;
			if (CarCount == 1) { result += personnelFee / 8; }
			if (CarCount == 2) { result += personnelFee / 6; }
			if (CarCount == 3) { result += personnelFee / 5; }
			if (CarCount > 3) { result += personnelFee / 4; }
			return result;
		}
	}

	/// <summary>
	/// 編成
	/// </summary>
	[Serializable()]
	public class Composition : IComposition
	{

		public string Name { get; set; }
		public int BestSpeed { get; set; }
		public bool IsElectrified => Power == PowerEnum.Electricity;
		public RailTypeEnum Type { get; set; }
		public CarGaugeEnum? Gauge { get; set; }
		public PowerEnum Power { get; set; }
		public CarTiltEnum Tilt { get; set; }
		public int HeldUnits { get; private set; }
		public int CarCount => Vehicles.Sum(vehicle => vehicle.Value);
		public virtual int PassengerCapacity => Vehicles.Sum(vehicle => vehicle.Key.PassengerCapacity * vehicle.Value);

		public void Purchase(GameInfo gameInfo, int quantity)
		{
			gameInfo.SpendMoney(Price * quantity);
			HeldUnits += quantity;
		}

		public void Use(int quantity)
		{
			if (quantity < 0) { throw new ArgumentException("数量は0以上で"); }

			if (HeldUnits < quantity)
			{
				throw new InvalidOperationException($"編成数量が{quantity - HeldUnits}つ不足しています");
			}
			HeldUnits -= quantity;
			return;
		}

		public void Release(int quantity) => HeldUnits += quantity;

		/// <summary>
		/// 構成車輌 (key:車両  value:構成両数)
		/// </summary>
		public Dictionary<Car, int> Vehicles { get; set; }

		/// <summary>
		/// 編成価格
		/// </summary>
		public int Price => Vehicles.Sum(vehicle => vehicle.Key.money * vehicle.Value);

		public SeatEnum? BestComfortSeat => Vehicles.OrderByDescending(v => v.Key.seat.ToComfortLevel()).FirstOrDefault().Key?.seat ?? null;
	}

	/// <summary>
	/// mode読み込み時にあてがう編成
	/// </summary>
	[Serializable()]
	public class DefautltComposition : IComposition
	{
		/// <summary>
		/// 座席の種類
		/// </summary>
		public SeatEnum seat;

		public int CarCount { get; set; }
		public int Price { get; set; }
		public int PassengerCapacity => GameConstants.SeatCapacity[seat] * CarCount;
		public int BestSpeed { get; set; }
		public CarGaugeEnum? Gauge { get; set; }
		public bool IsElectrified => Power == PowerEnum.Electricity;
		public string Name { get; set; }
		public PowerEnum Power { get; set; }
		public RailTypeEnum Type { get; set; }
		public int HeldUnits { get; internal set; }
		public CarTiltEnum Tilt { get; set; }

		public SeatEnum? BestComfortSeat => seat;

		public void Purchase(GameInfo gameInfo, int quantity)
		{
			gameInfo.SpendMoney(Price * quantity);
			HeldUnits += quantity;
		}

		public void Release(int quantity) => HeldUnits += quantity;

		public void Use(int quantity)
		{
			if (quantity < 0) { throw new ArgumentException("数量は0以上で"); }

			if (HeldUnits < quantity)
			{
				throw new InvalidOperationException($"編成数量が{quantity - HeldUnits}つ不足しています");
			}
			HeldUnits -= quantity;
			return;
		}
	}


	/// <summary>
	/// 車両
	/// </summary>
	[Serializable()]
	public class Car
	{
		/// <summary>
		/// 車両の名前
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 最高速度
		/// </summary>
		public int bestSpeed;

		/// <summary>
		/// 適合する路線のタイプ
		/// </summary>
		public RailTypeEnum type;

		/// <summary>
		/// 軌間
		/// </summary>
		public CarGaugeEnum gauge;

		/// <summary>
		/// 動力源
		/// </summary>
		public PowerEnum power;

		/// <summary>
		/// 座席
		/// </summary>
		public SeatEnum seat;

		/// <summary>
		/// 車体傾斜装置
		/// </summary>
		public CarTiltEnum carTilt;

		/// <summary>
		/// 車両価格
		/// </summary>
		public int money;

		/// <summary>
		/// 座席数
		/// </summary>
		public int PassengerCapacity => GameConstants.SeatCapacity[seat];
	}

	//駅
	[Serializable()]
	public class Station
	{
		/// <summary>
		/// 駅名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 駅の大きさ
		/// </summary>
		public StationSize Size { get; set; }

		/// <summary>
		/// 駅が属している路線
		/// </summary>
		public List<Line> BelongingLines { get; set; } = new List<Line>();

		/// <summary>
		/// 駅のX座標
		/// </summary>
		public int X
		{
			get => x; set
			{
				x = 0 <= value && value <= 980 ? value : throw new ArgumentException("駅のx座標は0~980");
			}
		}
		int x;

		/// <summary>
		/// 駅のY座標
		/// </summary>
		public int Y
		{
			get => y; set
			{
				y = 0 <= value && value <= 684 ? value : throw new ArgumentException("駅のy座標は0~684");
			}
		}
		int y;

		/// <summary>
		/// 駅周辺人口
		/// </summary>
		/// <remarks>0以上、単位は万人</remarks>
		public int Population
		{
			get => population; set
			{
				if (value < 0) { throw new ArgumentException("駅周辺人口は0以上"); }
				population = value;
			}
		}
		int population;

		/// <summary>
		/// 貨物の規模
		/// </summary>
		/// <remarks>0～255</remarks>
		public int KamotsuKibo
		{
			get => kamotsuKibo; set
			{
				if (0 <= value && value <= 255)
					kamotsuKibo = value;
				else
					throw new ArgumentException("駅貨物の規模は0~255");
			}
		}
		int kamotsuKibo;
	}

	/// <summary>
	/// 週次投資額
	/// </summary>
	[Serializable()]
	public struct InvestmentAmount
	{
		public InvestmentAmountEnum steam; //蒸気
		public InvestmentAmountEnum electricMotor; //電気モーター
		public InvestmentAmountEnum diesel; //ディーゼル
		public InvestmentAmountLinearEnum linearMotor; //リニアモータ
		public InvestmentAmountEnum newPlan; //新企画
	}

	/// <summary>
	/// 累計投資額(10万円単位)
	/// </summary>
	[Serializable()]
	public struct InvestmentAmountAccumulated
	{
		public long steam; //蒸気
		public long electricMotor; //電気モーター
		public long diesel; //ディーゼル
		public long linearMotor; //リニアモータ
		public long newPlan; //新企画
	}

	//乗り継ぎ
	[Serializable()]
	public class Longway
	{
		/// <summary>
		/// 経由ルート
		/// </summary>
		public List<Line> route;

		/// <summary>
		/// 始点
		/// </summary>
		public Station start;

		/// <summary>
		/// 終点
		/// </summary>
		public Station end;

		/// <summary>
		/// 貨物のルートとなるか
		/// </summary>
		public bool isKamotuOperated = true;

		/// <summary>
		/// 路線人口
		/// </summary>
		/// <returns></returns>
		public int LinePopulation => start.Population * end.Population / 10;

		/// <summary>
		/// 貨物運行本数を計算
		/// </summary>
		/// <returns></returns>
		public int CalcKamotuTrips(GameInfo gameInfo)
		{
			if (route.Any(line => line.IsExist == false)) { return 0; }

			int result = 0;
			switch (gameInfo.Kamotu)
			{
				case KamotsuEnum.DecreaseFrom1970:
					if (gameInfo.Year > (gameInfo.BasicYear + 70))
					{
						result = start.KamotsuKibo * end.KamotsuKibo / ((gameInfo.Year - gameInfo.BasicYear - 70) * (gameInfo.Year - gameInfo.BasicYear - 70) * 5 / 2500 + 5) - 2;
					}
					else
					{
						result = start.KamotsuKibo * end.KamotsuKibo / ((gameInfo.Year - gameInfo.BasicYear) / (-7) / 2 + 10) - 2;
					}
					break;
				case KamotsuEnum.EverIncrease:
					result = (start.KamotsuKibo + 5) * (end.KamotsuKibo + 5) * (gameInfo.Year - gameInfo.BasicYear + 100) / 1000;
					break;
				case KamotsuEnum.EverDecrease:
					result = (start.KamotsuKibo + 5) * (end.KamotsuKibo + 5) * 10 / (gameInfo.Year - gameInfo.BasicYear + 50);
					break;
				case KamotsuEnum.Nothing:
					result = 0;
					break;
			}
			result = Math.Max(0, result);
			if (gameInfo.modss != null) { result = gameInfo.modss.MultiplyKamotsu(result); }
			return result;
		}
	}
}
