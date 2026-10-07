using System;
using System.Collections.Generic;

namespace whr_wpf.Model
{
	/// <summary>
	/// Enumの属性読取と座席の計算用拡張
	/// </summary>
	public static class EnumExtentions
	{
		/// <summary>
		/// Enumの属性取得
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static T GetAttribute<T>(this Enum value) where T : Attribute
		{
			Type type = value.GetType();
			System.Reflection.MemberInfo[] memberInfo = type.GetMember(value.ToString());
			object[] attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
			return attributes.Length > 0 ? (T)attributes[0] : null;
		}

		/// <summary>
		/// 座席の乗り心地レベル取得
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int ToComfortLevel(this SeatEnum value)
		{
			ComfortLevelAttribute attribute = value.GetAttribute<ComfortLevelAttribute>();
			return attribute?.Level ?? throw new InvalidOperationException("座席の乗り心地レベルが未定義");
		}

		/// <summary>
		/// 座席の乗客数ボーナス取得
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int ToPassengerNumBonus(this SeatEnum value)
		{
			ComfortLevelAttribute attribute = value.GetAttribute<ComfortLevelAttribute>();
			return attribute?.PasserngerNumBonus ?? throw new InvalidOperationException("座席の乗客数ボーナスが未定義");
		}
	}

	/// <summary>
	/// ゲーム内定数
	/// </summary>
	public class GameConstants
	{
		/// <summary>
		/// 定着度のデフォルト値
		/// </summary>
		public const int RetentionRateDefault = 10000;

		/// <summary>
		/// 1両あたり座席数
		/// </summary>
		public static Dictionary<SeatEnum, int> SeatCapacity = new Dictionary<SeatEnum, int>
		{
			{SeatEnum.None, 60 },
			{SeatEnum.RetructableLong, 60 },
			{SeatEnum.Long, 55 },
			{SeatEnum.Dual, 55 },
			{SeatEnum.Semi, 50 },
			{SeatEnum.Convertible, 45 },
			{SeatEnum.DoubleDeckerRotatable, 33 },
			{SeatEnum.Rotatable, 22},
			{SeatEnum.DoubleDeckerRich, 20 },
			{SeatEnum.Rich, 13 }
		};
	}

	/// <summary>
	/// 難易度
	/// </summary>
	public enum DifficultyLevelEnum
	{
		VeryEasy = 1,

		Easy = 2,

		Normal = 3,

		Hard = 4,

		VeryHard = 5
	}

	/// <summary>
	/// 乗客数動態
	/// </summary>
	public enum SeasonEnum
	{
		JapanSightSeeing = 1,
		JapanCommuter = 2,
		Constantly = 3,
		Europian = 4
	}

	/// <summary>
	/// 貨物動態
	/// </summary>
	public enum KamotsuEnum
	{
		DecreaseFrom1970 = 1,
		EverIncrease = 2,
		EverDecrease = 3,
		Nothing = 4
	}

	/// <summary>
	/// 情報表示位置
	/// </summary>
	public enum InfoPosiEnum
	{
		TopLeft = 1,
		BottomLeft = 2,
		TopRight = 3,
		BottomRight = 4
	}

	/// <summary>
	/// 車両・編成の軌間
	/// </summary>
	public enum CarGaugeEnum
	{
		Narrow,

		Regular,

		FreeGauge
	}

	/// <summary>
	/// 動力
	/// </summary>
	public enum PowerEnum
	{
		Steam,

		Electricity,

		Diesel,

		LinearMotor
	}


	/// <summary>
	/// 車体傾斜装置
	/// </summary>
	public enum CarTiltEnum
	{
		/// <summary>
		/// 車体傾斜装置なし
		/// </summary>
		None,
		/// <summary>
		/// 振り子式
		/// </summary>
		Pendulum,
		/// <summary>
		/// 簡易機械式
		/// </summary>
		SimpleMecha,
		/// <summary>
		/// 高性能機械式
		/// </summary>
		HighMecha
	}


	/// <summary>
	/// 座席
	/// </summary>
	public enum SeatEnum
	{
		/// <summary>
		/// 座席なし
		/// </summary>
		[ComfortLevel(1, 0)]
		None = 1,

		/// <summary>
		/// 収容式ロングシート
		/// </summary>
		[ComfortLevel(2, 1500)]
		RetructableLong = 2,

		/// <summary>
		/// ロングシート
		/// </summary>
		[ComfortLevel(3, 1750)]
		Long = 3,

		/// <summary>
		/// デュアルシート
		/// </summary>
		[ComfortLevel(4, 4000)]
		Dual = 4,

		/// <summary>
		/// セミクロスシート
		/// </summary>
		[ComfortLevel(5, 4000)]
		Semi = 5,

		/// <summary>
		/// 転換クロスシート
		/// </summary>
		[ComfortLevel(6, 4300)]
		Convertible = 6,

		/// <summary>
		/// 二階建て回転クロス
		/// </summary>
		[ComfortLevel(8, 9000)]
		DoubleDeckerRotatable = 7,

		/// <summary>
		/// 回転クロスシート
		/// </summary>
		[ComfortLevel(8, 9000)]
		Rotatable = 8,

		/// <summary>
		/// 二階建て豪華クロス
		/// </summary>
		[ComfortLevel(10, 15000)]
		DoubleDeckerRich = 9,

		/// <summary>
		/// 豪華クロス
		/// </summary>
		[ComfortLevel(10, 15000)]
		Rich = 10,
	}

	/// <summary>
	/// 乗り心地レベル属性
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public class ComfortLevelAttribute : Attribute
	{
		public int Level { get; private set; }
		public int PasserngerNumBonus { get; private set; }
		public ComfortLevelAttribute(int level, int passengerNumBonus)
		{
			this.Level = level;
			this.PasserngerNumBonus = passengerNumBonus;
		}
	}


	/// <summary>
	/// 路線のタイプ
	/// </summary>
	public enum RailTypeEnum
	{
		Iron,
		LinearMotor
	}

	/// <summary>
	/// 路線の軌間
	/// </summary>
	public enum RailGaugeEnum
	{
		Narrow,
		Regular
	}

	/// <summary>
	/// 待避線間隔
	/// </summary>
	public enum TaihisenEnum
	{
		/// <summary>
		/// なし
		/// </summary>
		None,

		/// <summary>
		/// 100kmごと
		/// </summary>
		Every100km,

		/// <summary>
		/// 50kmごと
		/// </summary>
		Every50km,

		/// <summary>
		/// 20kmごと
		/// </summary>
		Every20km, //デフォ

		/// <summary>
		/// 10kmごと
		/// </summary>
		Every10km,

		/// <summary>
		/// 5kmごと
		/// </summary>
		Every5km,

		/// <summary>
		/// 2kmごと
		/// </summary>
		Every2km
	}

	/// <summary>
	/// ダイアグラム型
	/// </summary>
	public enum DiagramType
	{
		/// <summary>
		/// ダイヤ設定なし
		/// </summary>
		None = 0,

		/// <summary>
		/// 普通ダイヤ
		/// </summary>
		Regular = 1,

		/// <summary>
		/// 特急優先ダイヤ
		/// </summary>
		LimittedExpressPrior = 2, //デフォ

		/// <summary>
		/// 過密ダイヤ
		/// </summary>
		OverCrowded = 3,

		/// <summary>
		/// 並行ダイヤ
		/// </summary>
		Parallel = 4
	}

	/// <summary>
	/// 路線の位置づけ
	/// </summary>
	public enum LineGrade
	{
		/// <summary>
		/// 最重要幹線
		/// </summary>
		MostImportant = 1,

		/// <summary>
		/// 幹線
		/// </summary>
		Main = 2,

		/// <summary>
		/// 地方線
		/// </summary>
		Local = 3
	}

	/// <summary>
	/// 路線のタイプ
	/// </summary>
	public enum LinePropertyType
	{
		/// <summary>
		/// 日本都市間線(普通線)
		/// </summary>
		JapaneseInterCity,

		/// <summary>
		/// 郊外線
		/// </summary>
		Surburb,

		/// <summary>
		/// 近郊線
		/// </summary>
		Outskirts,

		/// <summary>
		/// 平野線
		/// </summary>
		Plain,

		/// <summary>
		/// 山地線
		/// </summary>
		Mountain,

		/// <summary>
		/// 山脈線
		/// </summary>
		Alpine,

		/// <summary>
		/// 海線
		/// </summary>
		Sea,

		/// <summary>
		/// ロシア平野線
		/// </summary>
		RussianPlain,

		/// <summary>
		/// 地下線
		/// </summary>
		Underground
	}

	/// <summary>
	/// 駅の大きさ
	/// </summary>
	public enum StationSize
	{
		/// <summary>
		/// 首都
		/// </summary>
		Capital = 20,

		/// <summary>
		/// 乗換駅
		/// </summary>
		Transit = 16,

		/// <summary>
		/// その他駅
		/// </summary>
		Other = 12
	}

	/// <summary>
	/// 週次投資額(リニア以外)
	/// </summary>
	public enum InvestmentAmountEnum
	{
		/// <summary>
		/// なし
		/// </summary>
		Nothing = 0,

		/// <summary>
		/// 2000万円
		/// </summary>
		MN2000 = 200,

		/// <summary>
		/// 5000万円
		/// </summary>
		MN5000 = 500,

		/// <summary>
		/// 1億円
		/// </summary>
		OK1 = 1000,

		/// <summary>
		/// 5億円
		/// </summary>
		OK5 = 5000,

		/// <summary>
		/// 10億円
		/// </summary>
		OK10 = 10000,

		/// <summary>
		/// 25億円
		/// </summary>
		OK25 = 25000,

		/// <summary>
		/// 50億円
		/// </summary>
		OK50 = 50000,

		/// <summary>
		/// 100億円
		/// </summary>
		OK100 = 100000,
	}

	/// <summary>
	/// 週次投資額(リニア)
	/// </summary>
	public enum InvestmentAmountLinearEnum
	{
		/// <summary>
		/// なし
		/// </summary>
		Nothing = 0,

		/// <summary>
		/// 10億円
		/// </summary>
		OK10 = 10000,

		/// <summary>
		/// 25億円
		/// </summary>
		OK25 = 25000,

		/// <summary>
		/// 50億円
		/// </summary>
		OK50 = 50000,

		/// <summary>
		/// 100億円
		/// </summary>
		OK100 = 100000,

		/// <summary>
		/// 250億円
		/// </summary>
		OK250 = 250000,

		/// <summary>
		/// 500億円
		/// </summary>
		OK500 = 500000,
	}

	/// <summary>
	/// 路線作成目標
	/// </summary>
	public enum LineGoalTargetEnum
	{
		/// <summary>
		/// 最重要幹線
		/// </summary>
		MostImportant = 1,

		/// <summary>
		/// 最重要幹線と幹線
		/// </summary>
		MostImportantAndMain = 2,

		/// <summary>
		/// 全線
		/// </summary>
		All = 3
	}
}
