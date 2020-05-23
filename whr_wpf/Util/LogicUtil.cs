using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using whr_wpf.Model;

namespace whr_wpf.Util
{
	/// <summary>
	/// ゲーム内ロジックユーティリティー
	/// </summary>
	class LogicUtil
	{
		/// <summary>
		/// modファイルで指定されたシートを内部のシートのIDに変換
		/// </summary>
		/// <param name="seat"></param>
		/// <returns></returns>
		public static SeatEnum ConvertSeatModToInternalId(int seat)
		{
			if (seat > 20) { return SeatEnum.Long; }
			if (seat == 8) { return SeatEnum.Rich; }
			if (seat == 7) { return SeatEnum.Rotatable; }
			if (Enum.IsDefined(typeof(SeatEnum), seat))
			{
				return (SeatEnum)seat;
			}
			else
			{
				throw new ArgumentException($"編成の座席タイプの指定が不正です 座席タイプ：{seat}");
			}
		}

		/// <summary>
		/// 乗車可能数計算
		/// </summary>
		/// <param name="composition">編成</param>
		/// <param name="honsuu">運行本数</param>
		/// <returns></returns>
		public static int CalcMaximumCapacity(IComposition composition, int honsuu)
		{
			if (composition is null) { return 0; }

			int sectionCapacity = composition.PassengerCapacity * honsuu;

			//標準軌は多めに乗れる設定
			if (composition.Type == RailTypeEnum.Iron && composition.Gauge == CarGaugeEnum.Regular)
			{
				return sectionCapacity * 4;
			}
			return sectionCapacity * 3;
		}

		/// <summary>
		/// 路線建造コスト計算ロジック
		/// </summary>
		/// <param name="bestSpeed"></param>
		/// <param name="laneSu"></param>
		/// <param name="distance"></param>
		/// <param name="railType"></param>
		/// <param name="railGauge"></param>
		/// <param name="isElectrified"></param>
		/// <param name="taihisen"></param>
		/// <param name="lineProperty"></param>
		/// <param name="gameInfo"></param>
		/// <returns></returns>
		public static long CalcLineConstructionCost(
			int bestSpeed, int laneSu, int distance,
			RailTypeEnum railType, RailGaugeEnum? railGauge, bool? isElectrified,
			TaihisenEnum taihisen, LinePropertyType lineProperty,
			GameInfo gameInfo)
		{
			if (gameInfo is null)
			{
				throw new ArgumentNullException(nameof(gameInfo));
			}

			int railCostKeisu = gameInfo.LineMakeCost, year = gameInfo.Year, basicYear = gameInfo.BasicYear, economicIndex = gameInfo.CalcEconomicIndex();

			long railCost;
			long taihiCost;
			int taihiKeisu;
			if (laneSu == 3) { laneSu = 4; }
			switch (lineProperty)
			{
				case LinePropertyType.JapaneseInterCity:
					railCost = (bestSpeed + 40) * (laneSu + 1) * (distance + 10) * 4;
					taihiKeisu = 400;
					break;
				case LinePropertyType.Surburb:
					railCost = (bestSpeed + 40) * (laneSu) * (distance + 1) * 6;
					taihiKeisu = 500;
					break;
				case LinePropertyType.Outskirts:
					railCost = (bestSpeed + 40) * (laneSu) * (distance + 1) * 8;
					taihiKeisu = 700;
					break;
				case LinePropertyType.Plain:
					railCost = (bestSpeed + 200) * (laneSu + 1) * (distance + 10) * 4 / 3;
					taihiKeisu = 300;
					break;
				case LinePropertyType.Mountain:
					if (bestSpeed > 80)
					{
						railCost = (bestSpeed + 200) * (laneSu + 1) * (distance + 10) * 4;
						taihiKeisu = 500;
					}
					else
					{
						railCost = (bestSpeed + 40) * (laneSu + 1) * (distance + 10) * 4;
						taihiKeisu = 400;
					}
					break;
				case LinePropertyType.Alpine:
					if (bestSpeed > 60)
					{
						railCost = (bestSpeed + 200) * (laneSu + 1) * (distance + 10) * 5;
						taihiKeisu = 550;
					}
					else
					{
						railCost = (bestSpeed + 30) * (laneSu + 1) * (distance + 10) * 5;
						taihiKeisu = 450;
					}
					break;
				case LinePropertyType.Sea:
					if (bestSpeed > 40)
					{
						railCost = (bestSpeed + 500) * (laneSu + 1) * (distance + 10) * 5;
						taihiKeisu = 800;
					}
					else
					{
						railCost = 240 * (laneSu + 1) * (distance + 10) * 5;
						taihiKeisu = 500;
					}
					break;
				case LinePropertyType.RussianPlain:
					railCost = (bestSpeed + 500) * (laneSu + 1) / 7 * (distance + 20) * 4;
					taihiKeisu = 200;
					break;
				case LinePropertyType.Underground:
					if (bestSpeed > 40)
					{
						railCost = (bestSpeed + 200) * (laneSu + 1) * (distance + 10) * 8;
						taihiKeisu = 200;
					}
					else
					{
						railCost = 240 * (laneSu + 1) * (distance + 10);
						taihiKeisu = 500;
					}
					break;
				default:
					throw new ArgumentException($"引数の内容が誤りです 内容:{lineProperty}", nameof(lineProperty));
			}

			//標準軌、リニア
			if (railType == RailTypeEnum.LinearMotor || railGauge == RailGaugeEnum.Regular) { railCost = railCost * 6 / 5; }

			//電化(リニアは電化)
			if (railType == RailTypeEnum.LinearMotor || isElectrified.GetValueOrDefault(true)) { railCost = railCost * 3 / 2; }

			//リニア地下鉄(都営大江戸線、福岡市営地下鉄七隈線みたいな)
			if (lineProperty == LinePropertyType.Underground && railType == RailTypeEnum.LinearMotor && bestSpeed < 200)
			{ railCost /= 2; }

			switch (taihisen)
			{
				case TaihisenEnum.None:
					taihiCost = 0;
					break;
				case TaihisenEnum.Every100km:
					taihiCost = (distance / 100 + 1) * taihiKeisu * laneSu;
					break;
				case TaihisenEnum.Every50km:
					taihiCost = (distance / 50 + 1) * taihiKeisu * laneSu;
					break;
				case TaihisenEnum.Every20km:
					taihiCost = (distance / 20 + 1) * taihiKeisu * laneSu;
					break;
				case TaihisenEnum.Every10km:
					taihiCost = (distance / 10 + 1) * taihiKeisu * laneSu;
					break;
				case TaihisenEnum.Every5km:
					taihiCost = (distance / 5 + 1) * taihiKeisu * laneSu;
					break;
				case TaihisenEnum.Every2km:
					taihiCost = (distance / 2 + 1) * taihiKeisu * laneSu;
					break;
				default:
					throw new ArgumentException($"引数の内容が誤りです 内容:{taihisen}", nameof(taihisen));
			}

			var totalCost = railCost + taihiCost;

			return totalCost / 100 * railCostKeisu / 188 * (year / 10 - basicYear / 10 + 188) / 10 * economicIndex / 10;
		}

		/// <summary>
		/// お金に単位付与
		/// </summary>
		/// <param name="amount">金額(10万円単位)</param>
		/// <returns></returns>
		public static string AppendMoneyUnit(long amount)
		{
			long trueAmount = Math.Abs(amount * 10_0000);

			return $"{ConvJapaneseNumeral(trueAmount)}円";
		}

		/// <summary>
		/// 日本の命数法に変換
		/// </summary>
		/// <param name="num">数字()</param>
		/// <returns></returns>
		private static string ConvJapaneseNumeral(long num)
		{
			if (num < 0) { throw new ArgumentException("数値は0以上を指定してください"); }

			int ichi = (int)(num % 1_0000),
				man = (int)(num % 1_0000_0000 / 1_0000),
				oku = (int)(num % 1_0000_0000_0000 / 1_0000_0000),
				cho = (int)(num % 1_0000_0000_0000_0000 / 1_0000_0000_0000);

			string result = (cho > 0 ? $"{cho}兆" : "")
				+ (oku > 0 ? $"{oku}億" : "")
				+ (man > 0 ? $"{man}万" : "")
				+ (ichi > 0 ? $"{ichi}" : "");
			if (ichi == 0 && man == 0 && oku == 0 && cho == 0) { result = "0"; }

			return result;
		}

		/// <summary>
		/// 車両購入コスト計算
		/// </summary>
		/// <param name="bestSpeed"></param>
		/// <param name="power"></param>
		/// <param name="gauge"></param>
		/// <param name="seat"></param>
		/// <param name="tilt"></param>
		/// <param name="genkaiLinear"></param>
		/// <param name="genkaiJoki"></param>
		/// <param name="genkaiDenki"></param>
		/// <param name="genkaiKidosha"></param>
		/// <returns></returns>
		public static int CalcPurchaseCarCost(int bestSpeed, PowerEnum power, CarGaugeEnum gauge, SeatEnum seat, CarTiltEnum tilt,
			int genkaiLinear, int genkaiJoki, int genkaiDenki, int genkaiKidosha)
		{
			// 8*速度^2
			int cost = 8 * bestSpeed * bestSpeed;

			if (power == PowerEnum.LinearMotor) { cost = cost * 6 / 5 / genkaiLinear; } //リニア
			else
			{
				if (gauge == CarGaugeEnum.Regular || gauge == CarGaugeEnum.FreeGauge) { cost = cost * 3 / 2; } //標準軌orフリーゲージ
				if (power == PowerEnum.Steam) { cost = cost * 6 / 5 / genkaiJoki; } //蒸気
				if (power == PowerEnum.Electricity) { cost /= genkaiDenki; } //電車
				if (power == PowerEnum.Diesel) { cost /= genkaiKidosha; } //ディーゼル
			}
			switch (tilt)
			{
				case CarTiltEnum.Pendulum:
					cost = cost * 6 / 5;
					break;
				case CarTiltEnum.SimpleMecha:
					cost = cost * 11 / 10;
					break;
				case CarTiltEnum.HighMecha:
					cost = cost * 6 / 5;
					break;
			}
			cost += 500;

			//シート別
			switch (seat)
			{
				case SeatEnum.None:
				case SeatEnum.Dual:
					cost += 100;
					break;
				case SeatEnum.RetructableLong:
					cost += 50;
					break;
				case SeatEnum.Long:
				case SeatEnum.Semi:
				case SeatEnum.Convertible:
					cost += 150;
					break;
				case SeatEnum.DoubleDeckerRotatable:
				case SeatEnum.DoubleDeckerRich:
					cost = cost * 4 / 3;
					break;
			}

			return cost;
		}
	}

	/// <summary>
	/// 編成ファクトリー
	/// </summary>
	public class CompositionFactory
	{
		public static (bool canCompositionMake, string msg) CheckMakeComposition(string name, IEnumerable<KeyValuePair<Car, int>> VehicleNums)
		{
			if (string.IsNullOrWhiteSpace(name)) { return (false, "名前が指定されていません"); }

			ImmutableDictionary<Car, int> kvDict = VehicleNums
				.Where(kvPair => kvPair.Value > 0)
				.ToImmutableDictionary(kvPair => kvPair.Key, kvPair => kvPair.Value);

			if (kvDict.IsEmpty) { return (false, "車両の指定がありません"); }

			var gauges = kvDict.Select(v => v.Key.gauge).Distinct();
			if (gauges.Contains(CarGaugeEnum.Narrow) && gauges.Contains(CarGaugeEnum.Regular))
			{
				return (false, "車両の軌間に違いがあります");
			}

			if (kvDict.Select(v => v.Key.type).Distinct().Count() != 1)
			{
				return (false, "車両の軌道タイプに違いがあります");
			}

			if (kvDict.Select(v => v.Key.power).Distinct().Count() != 1)
			{
				return (false, "車両の動力に違いがあります");
			}

			if (kvDict.Select(v => v.Key.carTilt).Distinct().Count() != 1)
			{
				return (false, "車両の車体傾斜装置に違いがあります");
			}

			int bestSpeed = CalcCompositionBestSpeed(kvDict);
			if (bestSpeed < 40)
			{
				return (false, "最高速度が40km/hに達していません");
			}

			return (true, null);
		}

		/// <summary>
		/// 編成をインスタンス化
		/// </summary>
		/// <param name="name"></param>
		/// <param name="VehicleNums"></param>
		/// <returns></returns>
		public static Composition CreateComposition(string name, IEnumerable<KeyValuePair<Car, int>> VehicleNums)
		{
			(bool canCompositionMake, string msg) = CheckMakeComposition(name, VehicleNums);

			if (canCompositionMake == false) { throw new InvalidOperationException(msg); }

			Composition result = new Composition();
			result.Vehicles = VehicleNums
				.Where(kvPair => kvPair.Value > 0)
				.ToDictionary(kvPair => kvPair.Key, kvPair => kvPair.Value);
			result.BestSpeed = CalcCompositionBestSpeed(result.Vehicles);

			var gauges = result.Vehicles.Select(v => v.Key.gauge).Where(g => g != CarGaugeEnum.FreeGauge).Distinct();
			result.Gauge = (gauges.Count()) switch
			{
				0 => CarGaugeEnum.FreeGauge,
				_ => gauges.First(),
			};

			result.Name = name;
			result.Type = result.Vehicles.Select(v => v.Key.type).First();
			result.Power = result.Vehicles.Select(v => v.Key.power).First();
			result.Tilt = result.Vehicles.Select(v => v.Key.carTilt).First();

			return result;
		}

		/// <summary>
		/// 編成の最高速度計算
		/// </summary>
		/// <param name="VehicleNums"></param>
		/// <returns></returns>
		public static int CalcCompositionBestSpeed(IEnumerable<KeyValuePair<Car, int>> VehicleNums)
		{
			if (VehicleNums.All(v => v.Value == 0)) { return 0; }
			return (int)Math.Sqrt(VehicleNums.Sum(vehicleNum => Math.Pow(vehicleNum.Key.bestSpeed, 2) * vehicleNum.Value) / VehicleNums.Sum(kv => kv.Value));
		}
	}
}
