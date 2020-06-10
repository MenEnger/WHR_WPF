using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace whr_wpf.Model
{
	/// <summary>
	/// Enumに表示名を付与した属性を読み取る拡張
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
		/// EnumのDisplay.Name属性取得
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ToName(this Enum value)
		{
			DisplayAttribute attribute = value.GetAttribute<DisplayAttribute>();
			return attribute == null ? value.ToString() : attribute.Name;
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
		[Display(Name = "超簡単")]
		VeryEasy = 1,

		[Display(Name = "簡単")]
		Easy = 2,

		[Display(Name = "普通")]
		Normal = 3,

		[Display(Name = "難しい")]
		Hard = 4,

		[Display(Name = "激むず")]
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
		[Display(Name = "狭軌")]
		Narrow,

		[Display(Name = "標準軌")]
		Regular,

		[Display(Name = "フリーゲージ")]
		FreeGauge
	}

	/// <summary>
	/// 動力
	/// </summary>
	public enum PowerEnum
	{
		[Display(Name = "蒸気")]
		Steam,

		[Display(Name = "電気")]
		Electricity,

		[Display(Name = "ディーゼル")]
		Diesel,

		[Display(Name = "リニアモーター")]
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
		[Display(Name = "なし")]
		None,
		/// <summary>
		/// 振り子式
		/// </summary>
		[Display(Name = "振り子式")]
		Pendulum,
		/// <summary>
		/// 簡易機械式
		/// </summary>
		[Display(Name = "簡易機械式")]
		SimpleMecha,
		/// <summary>
		/// 高性能機械式
		/// </summary>
		[Display(Name = "高性能機械式")]
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
		[Display(Name = "座席なし")]
		[ComfortLevel(1, 0)]
		None = 1,

		/// <summary>
		/// 収容式ロングシート
		/// </summary>
		[Display(Name = "収容式ロングシート")]
		[ComfortLevel(2, 1500)]
		RetructableLong = 2,

		/// <summary>
		/// ロングシート
		/// </summary>
		[Display(Name = "ロングシート")]
		[ComfortLevel(3, 1750)]
		Long = 3,

		/// <summary>
		/// デュアルシート
		/// </summary>
		[Display(Name = "デュアルシート")]
		[ComfortLevel(4, 4000)]
		Dual = 4,

		/// <summary>
		/// セミクロスシート
		/// </summary>
		[Display(Name = "セミクロスシート")]
		[ComfortLevel(5, 4000)]
		Semi = 5,

		/// <summary>
		/// 転換クロスシート
		/// </summary>
		[Display(Name = "転換クロスシート")]
		[ComfortLevel(6, 4300)]
		Convertible = 6,

		/// <summary>
		/// 二階建て回転クロス
		/// </summary>
		[Display(Name = "二階建て回転クロス")]
		[ComfortLevel(8, 9000)]
		DoubleDeckerRotatable = 7,

		/// <summary>
		/// 回転クロスシート
		/// </summary>
		[Display(Name = "回転クロスシート")]
		[ComfortLevel(8, 9000)]
		Rotatable = 8,

		/// <summary>
		/// 二階建て豪華クロス
		/// </summary>
		[Display(Name = "二階建て豪華クロス")]
		[ComfortLevel(10, 15000)]
		DoubleDeckerRich = 9,

		/// <summary>
		/// 豪華クロス
		/// </summary>
		[Display(Name = "豪華クロスシート")]
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
		[Display(Name = "なし")]
		None,

		/// <summary>
		/// 100kmごと
		/// </summary>
		[Display(Name = "100kmごと")]
		Every100km,

		/// <summary>
		/// 50kmごと
		/// </summary>
		[Display(Name = "50kmごと")]
		Every50km,

		/// <summary>
		/// 20kmごと
		/// </summary>
		[Display(Name = "20kmごと")]
		Every20km, //デフォ

		/// <summary>
		/// 10kmごと
		/// </summary>
		[Display(Name = "10kmごと")]
		Every10km,

		/// <summary>
		/// 5kmごと
		/// </summary>
		[Display(Name = "5kmごと")]
		Every5km,

		/// <summary>
		/// 2kmごと
		/// </summary>
		[Display(Name = "2kmごと")]
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
		[Display(Name = "ダイヤなし")]
		None = 0,

		/// <summary>
		/// 普通ダイヤ
		/// </summary>
		[Display(Name = "普通ダイヤ")]
		Regular = 1,

		/// <summary>
		/// 特急優先ダイヤ
		/// </summary>
		[Display(Name = "特急優先ダイヤ")]
		LimittedExpressPrior = 2, //デフォ

		/// <summary>
		/// 過密ダイヤ
		/// </summary>
		[Display(Name = "過密ダイヤ")]
		OverCrowded = 3,

		/// <summary>
		/// 並行ダイヤ
		/// </summary>
		[Display(Name = "並行ダイヤ")]
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
		[Display(Name = "最重要幹線")]
		MostImportant = 1,

		/// <summary>
		/// 幹線
		/// </summary>
		[Display(Name = "幹線")]
		Main = 2,

		/// <summary>
		/// 地方線
		/// </summary>
		[Display(Name = "地方線")]
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
		[Display(Name = "普通線")]
		JapaneseInterCity,

		/// <summary>
		/// 郊外線
		/// </summary>
		[Display(Name = "郊外線")]
		Surburb,

		/// <summary>
		/// 近郊線
		/// </summary>
		[Display(Name = "近郊線")]
		Outskirts,

		/// <summary>
		/// 平野線
		/// </summary>
		[Display(Name = "平野線")]
		Plain,

		/// <summary>
		/// 山地線
		/// </summary>
		[Display(Name = "山地線")]
		Mountain,

		/// <summary>
		/// 山脈線
		/// </summary>
		[Display(Name = "山脈線")]
		Alpine,

		/// <summary>
		/// 海線
		/// </summary>
		[Display(Name = "海線")]
		Sea,

		/// <summary>
		/// ロシア平野線
		/// </summary>
		[Display(Name = "ロシア平野線")]
		RussianPlain,

		/// <summary>
		/// 地下線
		/// </summary>
		[Display(Name = "地下線")]
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
		[Display(Name = "なし")]
		Nothing = 0,

		/// <summary>
		/// 2000万円
		/// </summary>
		[Display(Name = "2000万円")]
		MN2000 = 200,

		/// <summary>
		/// 5000万円
		/// </summary>
		[Display(Name = "5000万円")]
		MN5000 = 500,

		/// <summary>
		/// 1億円
		/// </summary>
		[Display(Name = "1億円")]
		OK1 = 1000,

		/// <summary>
		/// 5億円
		/// </summary>
		[Display(Name = "5億円")]
		OK5 = 5000,

		/// <summary>
		/// 10億円
		/// </summary>
		[Display(Name = "10億円")]
		OK10 = 10000,

		/// <summary>
		/// 25億円
		/// </summary>
		[Display(Name = "25億円")]
		OK25 = 25000,

		/// <summary>
		/// 50億円
		/// </summary>
		[Display(Name = "50億円")]
		OK50 = 50000,

		/// <summary>
		/// 100億円
		/// </summary>
		[Display(Name = "100億円")]
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
		[Display(Name = "なし")]
		Nothing = 0,

		/// <summary>
		/// 10億円
		/// </summary>
		[Display(Name = "10億円")]
		OK10 = 10000,

		/// <summary>
		/// 25億円
		/// </summary>
		[Display(Name = "25億円")]
		OK25 = 25000,

		/// <summary>
		/// 50億円
		/// </summary>
		[Display(Name = "50億円")]
		OK50 = 50000,

		/// <summary>
		/// 100億円
		/// </summary>
		[Display(Name = "100億円")]
		OK100 = 100000,

		/// <summary>
		/// 250億円
		/// </summary>
		[Display(Name = "250億円")]
		OK250 = 250000,

		/// <summary>
		/// 500億円
		/// </summary>
		[Display(Name = "500億円")]
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
		[Display(Name = "最重要幹線")]
		MostImportant = 1,

		/// <summary>
		/// 最重要幹線と幹線
		/// </summary>
		[Display(Name = "最重要幹線及び幹線")]
		MostImportantAndMain = 2,

		/// <summary>
		/// 全線
		/// </summary>
		[Display(Name = "全線")]
		All = 3
	}
}
