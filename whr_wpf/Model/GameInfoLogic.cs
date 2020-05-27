using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace whr_wpf.Model
{
	public partial class GameInfo : INotifyPropertyChanged
	{

		/// <summary>
		/// 蒸気機関への投資が可能か
		/// </summary>
		/// <returns></returns>
		public bool CanSteamDevelop() => genkaiJoki < 150 && Year < SteamYear;

		/// <summary>
		/// 電気モーターへの投資が可能か
		/// </summary>
		/// <returns></returns>
		public bool CanElectricMotorDevelop() => genkaiJoki >= 80 && genkaiDenki < 990;

		/// <summary>
		/// 気動車への投資が可能か
		/// </summary>
		/// <returns></returns>
		public bool CanDieselDevelop() => genkaiDenki >= 80 && genkaiKidosha < 990;

		/// <summary>
		/// リニアへの投資が可能か
		/// </summary>
		/// <returns></returns>
		public bool CanLinearMotorDevelop() => (genkaiDenki >= 200 || genkaiKidosha >= 200) && genkaiLinear < 990;

		/// <summary>
		/// 新企画への投資が可能か
		/// </summary>
		/// <returns></returns>
		/// 何かが未開発だと新企画開発できる
		public bool CanNewPlanDevelop() => !isDevelopedBlockingSignal
									 || !isDevelopedConvertibleCross
									 || !isDevelopedAutoGate
									 || !isDevelopedCarTiltPendulum
									 || !isDevelopedRichCross
									 || !isDevelopedRetructableLong
									 || !isDevelopedDualSeat
									 || !isDevelopedMachineTilt
									 || !isDevelopedFreeGauge
									 || !isDevelopedDynamicSignal;

		/// <summary>
		/// 指定の車両が開発可能か
		/// </summary>
		/// <returns>開発可否 メッセージ</returns>
		public (bool CanCreateVehicle, string msg) CheckCreateVehicle(string Name, int BestSpeed, PowerEnum Power, CarGaugeEnum Gauge, SeatEnum Seat, CarTiltEnum tilt)
		{
			if (string.IsNullOrWhiteSpace(Name)) { return (false, "名無しの権兵衛です"); }

			if (Power == PowerEnum.LinearMotor)
			{
				if (genkaiLinear == 0) { return (false, "リニアは作れません"); }
				else if (BestSpeed > genkaiLinear) { return (false, "速すぎます"); }
			}

			if (Gauge == CarGaugeEnum.FreeGauge && !isDevelopedFreeGauge) { return (false, "フリーゲージは作れません"); }

			if (Power == PowerEnum.Steam)
			{
				if (!IsSteamAvailable()) { return (false, "時代遅れです"); }
				if (BestSpeed > genkaiJoki) { return (false, "速すぎます"); }
			}

			if (Power == PowerEnum.Electricity)
			{
				if (genkaiDenki == 0) { return (false, "電車は作れません"); }
				else if (BestSpeed > genkaiDenki) { return (false, "速すぎます"); }
			}

			if (Power == PowerEnum.Diesel)
			{
				if (genkaiKidosha == 0) { return (false, "ディーゼルは作れません"); }
				else if (BestSpeed > genkaiKidosha) { return (false, "速すぎます"); }
			}

			if (Seat == SeatEnum.Dual && !isDevelopedDualSeat) { return (false, "デュアル不可"); }
			if (Seat == SeatEnum.Convertible && !isDevelopedConvertibleCross) { return (false, "転換式クロス不可"); }
			if (Seat == SeatEnum.RetructableLong && !isDevelopedRetructableLong) { return (false, "収納式ロング不可"); }
			if (Seat == SeatEnum.Rich && !isDevelopedRichCross) { return (false, "豪華クロス不可"); }
			if (Seat == SeatEnum.DoubleDeckerRich && !isDevelopedRichCross) { return (false, "豪華クロス不可"); }

			switch (tilt)
			{
				case CarTiltEnum.Pendulum:
					if (!isDevelopedCarTiltPendulum) { return (false, "振り子式車体傾斜装置は未開発"); }
					break;
				case CarTiltEnum.SimpleMecha:
				case CarTiltEnum.HighMecha:
					if (!isDevelopedMachineTilt) { return (false, "機械式式車体傾斜装置は未開発"); }
					break;
			}

			return (true, "");
		}

		/// <summary>
		/// 車両開発
		/// </summary>
		/// <param name="name"></param>
		/// <param name="bestSpeed"></param>
		/// <param name="power"></param>
		/// <param name="gauge"></param>
		/// <param name="seat"></param>
		/// <param name="tilt"></param>
		public void DevelopVehicle(string name, int bestSpeed, PowerEnum power, CarGaugeEnum gauge, SeatEnum seat, CarTiltEnum tilt)
		{
			(bool can, _) = CheckCreateVehicle(name, bestSpeed, power, gauge, seat, tilt);

			if (!can) { throw new InvalidOperationException("車両を開発可能な技術が揃っていません"); }

			SpendMoney(CalcDevelopVehicleCost(bestSpeed, power, gauge, seat, tilt));

			vehicles.Add(new Car
			{
				Name = name,
				bestSpeed = bestSpeed,
				power = power,
				gauge = gauge,
				seat = seat,
				carTilt = tilt,
				type = power == PowerEnum.LinearMotor ? RailTypeEnum.LinearMotor : RailTypeEnum.Iron,
				money = CalcPurchaseVehicleCost(bestSpeed, power, gauge, seat, tilt),
			});
		}

		/// <summary>
		/// お金の増減周りの処理まとめ 
		/// 引数を負の数にすると支出側に反映させる
		/// </summary>
		/// <param name="increment"></param>
		private void AddMoney(int increment)
		{
			Money += increment;

			if (increment >= 0)
			{
				income += increment;
			}
			else
			{
				outlay += Math.Abs(increment);
			}
		}

		/// <summary>
		/// 次週へ
		/// </summary>
		/// <remarks>解析難易度が高かった処理</remarks>
		/// <returns>処理結果メッセージのリスト</returns>
		public List<string> NextWeek()
		{
			//処理結果メッセージリスト
			var resultMsgList = new List<string>();

			//1区間利用者(短距離旅客)
			foreach (Line line in lines)
			{
				line.passengersLastWeek = line.CalcPassengersNumOnlyLine(true, this);
			}

			//乗り継ぎ(長距離旅客)
			Dictionary<Longway, int> kari9 = new Dictionary<Longway, int>(), kari10 = new Dictionary<Longway, int>();
			foreach (Longway longway in longwayList)
			{
				//路線未敷設or乗客0路線があれば計算せず終了
				if (longway.route.Where(line => line.IsExist == false || line.CalcPassengersNumOnlyLine(true, this) == 0).Count() > 0)
				{
					kari9[longway] = 0;
					kari10[longway] = 0;
					continue;
				}

				int worstRetentionRate = 99999, minimumNumOfTrips = int.MaxValue, totalRequiredMin = 0, totalDistance = 0;
				int passengersBonusBySeat;
				LinePropertyType? linePropertyType = null;
				//worstRetentionRate最低定着率 minimumNumOfTrips最低本数 totalRequiredMin総所要時間 passengersBonusBySeat乗り心地の一番低い値の乗客ボーナス totalDistance総距離 linePropertyType路線属性タイプ

				worstRetentionRate = longway.route.Select(line => line.retentionRate).Min();
				minimumNumOfTrips = longway.route.Select(line => line.TotalNumberTrips(false)).Min();
				totalRequiredMin = longway.route.Select(line => line.CalcAverageRequireMinutes()).Sum();
				passengersBonusBySeat = longway.route
					.Where(line => line.WorstComfortLevelSeat().HasValue)
					.Select(line => line.WorstComfortLevelSeat().Value.ToPassengerNumBonus())
					.Min();
				totalDistance = longway.route.Select(line => line.Distance).Sum();

				//路線タイプが1か2か8でないものがあれば、その乗り継ぎ区間では0扱い。優先順位は1,2,8以外>1>2>8の順
				linePropertyType = longway.route.All(line => line.propertyType == LinePropertyType.Underground)
					? (LinePropertyType?)LinePropertyType.Underground
					: longway.route.All(line =>
						line.propertyType == LinePropertyType.Underground || line.propertyType == LinePropertyType.Outskirts)
					? (LinePropertyType?)LinePropertyType.Outskirts
					: longway.route.All(line =>
						line.propertyType == LinePropertyType.Underground || line.propertyType == LinePropertyType.Outskirts || line.propertyType == LinePropertyType.Surburb)
					? (LinePropertyType?)LinePropertyType.Surburb
					: (LinePropertyType?)LinePropertyType.JapaneseInterCity;

				int kari = 0;
				//often 1 運行頻度スコア
				kari = minimumNumOfTrips * (Year - (BasicYear - 80)) + (50 * (BasicYear + 120 - Year)) / 2;
				kari = Math.Min(kari, 10000);

				//speed 6 速達スコア
				if (totalRequiredMin < 10) { kari += 40000; }
				else if (10 <= totalRequiredMin && totalRequiredMin < 410) { kari += 40000 - ((totalRequiredMin - 10) * 100); }
				if (totalRequiredMin < 60) { kari += 21000 - ((totalRequiredMin + 10) * 300); }
				kari = Math.Max(0, kari);

				//car 3 車両スコア 
				kari += passengersBonusBySeat;

				kari9[longway] = (int)Math.Pow((double)kari / 5000, 7.0);
				if (kari9[longway] < 0) { throw new InvalidOperationException("エラー(No.20)が発生しました。作者まで報告ください"); }

				kari = kari / 100 * worstRetentionRate / 10000 * longway.LinePopulation / 6000 * Rpm / 100;
				switch (linePropertyType)
				{
					case LinePropertyType.Surburb:
						kari *= 2;
						break;
					case LinePropertyType.Outskirts:
						kari *= 3;
						break;
					case LinePropertyType.Underground:
						kari *= 4;
						break;
				}
				kari10[longway] = kari * CalcEconomicIndex() / 100;
			}

			//競合処理
			//競合乗り継ぎ区間をグルーピング
			Dictionary<string, List<Longway>> rivalGroupList = longwayList.GroupBy(k =>
			{
				//乗り継ぎの定義によって始点と終点が逆転していても区間を一意にまとめられるように、ソートしてからキー生成
				List<string> names = new List<string>() { k.start.Name, k.end.Name };
				names.Sort();
				return string.Join("___", names);
			}).ToDictionary(kv => kv.Key, kv => kv.ToList());

			//競合のシェア比を計算
			foreach (KeyValuePair<string, List<Longway>> rivalList in rivalGroupList)
			{
				//競合の利用客数合計
				int passenngerSum = rivalList.Value.Sum(longway => kari9[longway]);
				if (passenngerSum == 0) { continue; }

				//合計が大きすぎる場合は桁数落とし
				while (passenngerSum >= 10000000)
				{
					passenngerSum /= 10;
					rivalList.Value.ForEach(longway => kari9[longway] /= 10);
				}
				//比率を書き込み
				rivalList.Value.ForEach(longway => kari9[longway] = kari9[longway] * 100 / passenngerSum);
			}

			//乗り継ぎの乗客数にシェア比を反映させて路線の乗客数に足し込み
			foreach (var longway in longwayList)
			{
				//未敷設区間あれば乗り継ぎ客が生じないのでスキップ
				if (longway.route.Any(line => line.IsExist == false)) { continue; }

				if (kari10[longway] <= 0) { continue; }

				//オリジナルではここでkari11が出てくるが、kari11は宣言されてからここまでkari11は一度も書き込まれていないため、必ず0のはず
				//kari9(比率)の誤りだと思われる
				//人数に競合とのシェア比を反映
				kari10[longway] = kari10[longway] * kari9[longway] / 100;
				longway.route.ForEach(line => line.passengersLastWeek += kari10[longway]);
			}

			//季節偏差と難易度などを反映させる
			foreach (Line line in lines)
			{
				if (line.IsExist == false) { continue; }
				switch (Difficulty)
				{
					case DifficultyLevelEnum.Hard:
						line.passengersLastWeek = line.passengersLastWeek * 49 / 50;
						if ((Year > (MYear + 80)) && line.bestSpeed < 100) { line.passengersLastWeek = line.passengersLastWeek * 3 / 4; }
						if ((Year > (MYear + 100)) && line.bestSpeed < 160) { line.passengersLastWeek = line.passengersLastWeek * 5 / 6; }
						break;
					case DifficultyLevelEnum.VeryHard:
						line.passengersLastWeek = line.passengersLastWeek * 19 / 20;
						if ((Year > (MYear + 80)) && line.bestSpeed < 100) { line.passengersLastWeek = line.passengersLastWeek / 2; }
						if ((Year > (MYear + 100)) && line.bestSpeed < 160) { line.passengersLastWeek = line.passengersLastWeek * 3 / 4; }
						break;
				}
				switch (Season)
				{
					case SeasonEnum.JapanSightSeeing:
						if (Month == 1 && Week == 1) { line.passengersLastWeek = line.passengersLastWeek * 11 / 10; }
						if (Month == 1 && Week == 2) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 1 && Week == 3) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 3) { line.passengersLastWeek = line.passengersLastWeek * 21 / 20; }
						if (Month == 5 && Week == 1) { line.passengersLastWeek = line.passengersLastWeek * 6 / 5; }
						if (Month == 5 && Week == 2) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 5 && Week == 3) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 5 && Week == 4) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 6) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 7) { line.passengersLastWeek = line.passengersLastWeek * 21 / 20; }
						if (Month == 8 && Week == 1) { line.passengersLastWeek = line.passengersLastWeek * 11 / 10; }
						if (Month == 8 && Week == 2) { line.passengersLastWeek = line.passengersLastWeek * 11 / 10; }
						if (Month == 8 && Week == 3) { line.passengersLastWeek = line.passengersLastWeek * 21 / 20; }
						if (Month == 8 && Week == 4) { line.passengersLastWeek = line.passengersLastWeek * 21 / 20; }
						if (Month == 9) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 12 && Week == 3) { line.passengersLastWeek = line.passengersLastWeek * 21 / 20; }
						if (Month == 12 && Week == 4) { line.passengersLastWeek = line.passengersLastWeek * 11 / 10; }
						break;
					case SeasonEnum.JapanCommuter:
						line.passengersLastWeek = line.passengersLastWeek * 21 / 20;
						if (Month == 1 && Week == 1) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 3) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 5 && Week == 1) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 7 && Week == 3) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 7 && Week == 4) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 8 && Week == 1) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 8 && Week == 2) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 8 && Week == 3) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 8 && Week == 4) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 12 && Week == 3) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 12 && Week == 4) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						break;
					case SeasonEnum.Constantly:
						break;
					case SeasonEnum.Europian:
						if (Month == 1 || Month == 2 || Month == 11 || Month == 12) { line.passengersLastWeek = line.passengersLastWeek * 9 / 10; }
						if (Month == 10) { line.passengersLastWeek = line.passengersLastWeek * 19 / 20; }
						if (Month == 4 || Month == 5 || Month == 6) { line.passengersLastWeek = line.passengersLastWeek * 21 / 20; }
						if (Month == 7) { line.passengersLastWeek = line.passengersLastWeek * 11 / 10; }
						if (Month == 8) { line.passengersLastWeek = line.passengersLastWeek * 6 / 5; }
						break;
				}
			}

			//貨物本数計算
			foreach (Longway longway in longwayList)
			{
				//未敷設区間あれば貨物が生じないのでスキップ
				if (longway.route.Any(line => line.IsExist == false)) { continue; }

				int kamotsu = longway.CalcKamotuTrips(this) * CalcEconomicIndex() / 100;

				longway.route.ForEach(line => line.kamotsuNumLastWeek += kamotsu);
			}

			//定着率調整
			lines.ForEach(line => line.AdjustRetentionRate());

			//旅客収入計算
			foreach (Line line in lines)
			{
				line.CalcAndReflectIncome(this);
			}

			//貨物便と旅客便の本数調整
			int kamotsuTanka = new Random().Next(35, 45);
			foreach (Line line in lines)
			{
				if (line.IsExist == false || line.kamotsuNumLastWeek == 0) { continue; }
				int excess = line.CalcExcessCapacity(false, genkaikyoyo);
				if (line.kamotsuNumLastWeek > excess)
				{
					if (modss == null)
					{
						//非戦時体制： 輸送量限界超え=true 余剰本数をそのまま貨物本数に
						line.isOverCapacity = true;
						line.kamotsuNumLastWeek = excess;
						continue;
					}
					else
					{
						//戦時体制： キャパに収まるように旅客便の本数を削っていく
						int overNum = line.kamotsuNumLastWeek - excess;
						if (line.runningPerDay > 0)
						{
							line.runningPerDay -= overNum;
							overNum = 0;
						}
						else if (line.runningPerDay < 0)
						{
							overNum = Math.Abs(line.runningPerDay);
							line.runningPerDay = 0;
						}
						if (overNum > 0)
						{
							//路線ダイヤを削ってもオーバーしたら、系統も充足するまで削る
							foreach (KeitoDiagram keito in line.belongingKeitoDiagrams)
							{
								keito.runningPerDay -= overNum;
								overNum = 0;
								if (keito.runningPerDay < 0)
								{
									overNum = Math.Abs(keito.runningPerDay);
									keito.runningPerDay = 0;
								}
								if (overNum == 0) { break; }
							}
						}
						if (overNum > 0)
						{
							//旅客便を削ってもキャパに収まらないなら限界がそのまま貨物の本数
							line.kamotsuNumLastWeek = line.GenkaiHonsuuUnderCurrent(genkaikyoyo);
							line.isOverCapacity = true;
						}
					}
				}
				else
				{
					line.isOverCapacity = false;
				}
			}

			//貨物収入計算と反映
			lines.ForEach(line => line.incomeLastWeek += line.CalcKamotsuFare(kamotsuTanka));

			//所持金に収入を反映
			Money += lines.Sum(line => line.incomeLastWeek);
			Money = Math.Min(Money, 2010000000);

			//支出計算
			lines.ForEach(line =>
			{
				int cost = line.CalcOutcome(this);
				line.outlayLastWeek = cost;
				Money -= cost;
			});

			//全路線の支出と収入をその週の収入と支出に反映、路線の総合収支にも反映
			income = (int)lines.Sum(line => line.incomeLastWeek);
			outlay = (int)lines.Sum(line => line.outlayLastWeek);
			lines.ForEach(line => line.totalBalance += (line.incomeLastWeek - line.outlayLastWeek));

			//政府補助金
			if (HojoStartYear <= Year && Year <= HojoEndYear && HojoAmount != 0)
			{
				Money += HojoAmount;
				if (HojoAmount < 0) { outlay -= HojoAmount; } else { income += HojoAmount; }
			}

			//技術開発
			int steamAmount = (int)weeklyInvestment.steam;
			AddMoney(-steamAmount);
			AccumulatedInvest.steam += steamAmount;

			int electAmount = (int)weeklyInvestment.electricMotor;
			AddMoney(-electAmount);
			AccumulatedInvest.electricMotor += electAmount;

			int dieselAmount = (int)weeklyInvestment.diesel;
			AddMoney(-dieselAmount);
			AccumulatedInvest.diesel += dieselAmount;

			int linearAmount = (int)weeklyInvestment.linearMotor;
			AddMoney(-linearAmount);
			AccumulatedInvest.linearMotor += linearAmount;

			int newPlanAmount = (int)weeklyInvestment.newPlan;
			AddMoney(-newPlanAmount);
			AccumulatedInvest.newPlan += newPlanAmount;

			OnPropertyChanged(nameof(AccumulatedInvest));

			//蒸気
			if (AccumulatedInvest.steam > Math.Pow(genkaiJoki - 30, 3) / 5 * TechCost / 4)
			{
				genkaiJoki += 5;
				resultMsgList.Add($"蒸気機関の改良が完了しました\n蒸気機関車の開発可能速度が{genkaiJoki}km/hになります。");
				if (genkaiJoki == 150) { weeklyInvestment.steam = InvestmentAmountEnum.Nothing; }
			}

			//電車
			if (AccumulatedInvest.electricMotor > Math.Pow(genkaiDenki + 10, 3) / 10 * TechCost / 2)
			{
				if (AccumulatedInvest.electricMotor > (3000 * TechCost) && genkaiDenki == 0)
				{
					genkaiDenki = 60;
					resultMsgList.Add("電気モーターが完成しました\n電車が作成できるようになります");
				}
				else if (0 < genkaiDenki && genkaiDenki < 360)
				{
					genkaiDenki += 5;
					resultMsgList.Add($"電気モーターが改良されました\n電車の開発可能速度が{genkaiDenki}km/hになります");
				}
				if (genkaiDenki == 360) { weeklyInvestment.electricMotor = InvestmentAmountEnum.Nothing; }
			}
			if (genkaiDenki >= 360 && AccumulatedInvest.electricMotor > (50000 * genkaiDenki + 30377125) / 10 * TechCost / 2)
			{
				genkaiDenki += 10;
				resultMsgList.Add("電気モーターが改良されました\n電車の作成コストが下がります");
			}
			if (genkaiDenki == 990) { weeklyInvestment.electricMotor = InvestmentAmountEnum.Nothing; }

			//ディーゼル
			if (AccumulatedInvest.diesel > Math.Pow(genkaiKidosha + 30, 3) / 10 * TechCost)
			{
				if (AccumulatedInvest.diesel > (3000 * TechCost) && genkaiKidosha == 0)
				{
					genkaiKidosha = 40;
					resultMsgList.Add("ディーゼル機関が完成しました\nディーゼルカーが作成できるようになります");
				}
				else if (0 < genkaiKidosha)
				{
					genkaiKidosha += 5;
					resultMsgList.Add($"ディーゼル機関が改良されました\nディーゼルカーの開発可能速度が{genkaiKidosha}km/hになります");
				}
				if (genkaiKidosha == 360) { weeklyInvestment.diesel = InvestmentAmountEnum.Nothing; }
			}
			if (genkaiKidosha >= 360 && AccumulatedInvest.diesel > (100000 * genkaiKidosha + 82638000) / 20 * TechCost)
			{
				genkaiKidosha += 10;
				resultMsgList.Add("ディーゼル機関が改良されました\nディーゼルカーの作成コストが下がります");
			}
			if (genkaiKidosha == 990) { weeklyInvestment.diesel = InvestmentAmountEnum.Nothing; }

			//リニア
			if (AccumulatedInvest.linearMotor > (375000 * TechCost) && genkaiLinear == 0)
			{
				genkaiLinear = 300;
				resultMsgList.Add("リニアが完成しました\nリニアカーが作成できるようになります");
			}
			else if (0 < genkaiLinear && AccumulatedInvest.linearMotor > Math.Pow(genkaiLinear - 100, 3) / 20 * TechCost)
			{
				genkaiLinear += 10;
				resultMsgList.Add($"リニアが改良されました\nリニアの開発可能速度が{genkaiLinear}km/hになります");
			}
			if (genkaiLinear == 990) { weeklyInvestment.linearMotor = InvestmentAmountLinearEnum.Nothing; }

			//新企画
			if (AccumulatedInvest.newPlan >= 1000 * TechCost && !isDevelopedBlockingSignal)
			{
				resultMsgList.Add("閉塞信号が完成しました\n運行可能数が増加します");
				genkaikyoyo += 5;
				isDevelopedBlockingSignal = true;
			}
			if (AccumulatedInvest.newPlan >= 5000 * TechCost && !isDevelopedConvertibleCross)
			{
				resultMsgList.Add("転換クロスシートが完成しました\n通勤列車にも使えるクロスシートです");
				isDevelopedConvertibleCross = true;
			}
			if (AccumulatedInvest.newPlan >= 10000 * TechCost && !isDevelopedAutoGate)
			{
				resultMsgList.Add("自動改札機が完成しました\n客一人当たりのコストが下がります");
				isDevelopedAutoGate = true;
			}
			if (AccumulatedInvest.newPlan >= 20000 * TechCost && !isDevelopedCarTiltPendulum)
			{
				resultMsgList.Add("振子式車体傾斜装置が完成しました\n対応車では、路線最高速度を20%上越えることができます");
				isDevelopedCarTiltPendulum = true;
			}
			if (AccumulatedInvest.newPlan >= 30000 * TechCost && !isDevelopedRichCross)
			{
				resultMsgList.Add("豪華クロスシートが完成しました\n最高の乗り心地を保障する座席です");
				isDevelopedRichCross = true;
			}
			if (AccumulatedInvest.newPlan >= 50000 * TechCost && !isDevelopedRetructableLong)
			{
				resultMsgList.Add("収納式ロングシートが完成しました\n普通のロングシートよりも定員数が多くなります");
				isDevelopedRetructableLong = true;
			}
			if (AccumulatedInvest.newPlan >= 200000 * TechCost && !isDevelopedDualSeat)
			{
				resultMsgList.Add("デュアルシートが完成しました\n乗り心地と定員数を両立させた座席です");
				isDevelopedDualSeat = true;
			}
			if (AccumulatedInvest.newPlan >= 300000 * TechCost && !isDevelopedMachineTilt)
			{
				resultMsgList.Add("機械式車体傾斜装置が完成しました\n①簡易タイプでは、振子式と同性能で価格が安くなります。\n②高性能タイプは、路線最高速度を33%越えることができます。");
				isDevelopedMachineTilt = true;
			}
			if (AccumulatedInvest.newPlan >= 500000 * TechCost && !isDevelopedFreeGauge)
			{
				resultMsgList.Add("フリーゲージトレインが完成しました\nフリーゲージトレインは、狭軌・標準軌関係なく走ることができます");
				isDevelopedFreeGauge = true;
			}
			if (AccumulatedInvest.newPlan >= 800000 * TechCost && !isDevelopedDynamicSignal)
			{
				resultMsgList.Add("移動閉塞信号が完成しました。\n運行可能数が増加します");
				isDevelopedDynamicSignal = true;
				genkaikyoyo += 5;
				weeklyInvestment.newPlan = InvestmentAmountEnum.Nothing;
			}

			//一週間プラス
			Week++;
			if (Week == 5)
			{
				Month++; Week = 1;
			}
			if (Month == 13)
			{
				//年次処理
				Year++; Month = 1;
				resultMsgList.AddRange(NextYear());

			}

			//経済動向変化
			economyTrends[0] += 360.0 / (4 * 40); //40ヶ月周期
			economyTrends[1] += 360.0 / (4 * 12 * 10); //10年周期
			economyTrends[2] += 360.0 / (4 * 12 * 20); //20年周期
			economyTrends[3] += 360.0 / (4 * 12 * 60); //60年周期

			return resultMsgList;

		}

		/// <summary>
		/// 年次処理
		/// </summary>
		private List<string> NextYear()
		{
			List<string> resultMsgList = new List<string>();

			if (Year == SteamYear) { resultMsgList.Add("今年から、蒸気機関車の設定が不可能になります。\n（現在設定中のものは引き続き使用可能です）"); }

			//人口設定
			Ap = MultiplyNumByDifficuluty(Ap);

			//オリジナルの都市人口増加ロジックが未解明なため、一律増で仮置き
			stations.ForEach(station => station.Population = MultiplyNumByDifficuluty(station.Population));
			//首都だけは1%ボーナスあり
			stations.Where(station => station.Size == StationSize.Capital).ToList()
				.ForEach(station => station.Population = station.Population / 10 * 101 / 10);

			//人口0都市の防止
			stations.Where(station => station.Population == 0).ToList()
				.ForEach(station => station.Population = 1);

			//戦時体制
			if (modss == null)
			{
				WarMode warMode = warModeList.FirstOrDefault(warMode => warMode.StartYear == Year);
				if (warMode != null)
				{
					modss = warMode;
					resultMsgList.Add("今年より戦時体制に突入します。\n貨物取扱量が変化し、貨物輸送を削減することができなくなります。");
				}
			}
			else
			{
				if (modss.EndYear == Year)
				{
					modss = null;
					resultMsgList.Add("戦時体制は終了しました");
				}
			}

			return resultMsgList;
		}

		/// <summary>
		/// 難易度によって数値を微増 総人口増加で使う
		/// </summary>
		/// <param name="num"></param>
		/// <returns></returns>
		private int MultiplyNumByDifficuluty(int num)
		{
			switch (Difficulty)
			{
				case DifficultyLevelEnum.VeryEasy:
					return num * 253 / 250;
				case DifficultyLevelEnum.Easy:
					return Year < BasicYear + 100 ? num * 253 / 250 : num * 201 / 200;
				case DifficultyLevelEnum.Normal:
				case DifficultyLevelEnum.Hard:
				case DifficultyLevelEnum.VeryHard:
					return Year < BasicYear + 100 ? num * 253 / 250 : num;
				default:
					return num;
			}
		}

		/// <summary>
		/// 編成購入
		/// </summary>
		/// <param name="quantity"></param>
		public void BuyComposition(IComposition composition, int quantity)
		{
			composition.Purchase(this, quantity);
		}

	}

}
