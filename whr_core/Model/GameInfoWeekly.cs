using System;
using System.Collections.Generic;
using System.Linq;

namespace whr_wpf.Model
{
	public partial class GameInfo
	{
		/// <summary>
		/// 今週の路線状態を初期化する。累計収支・定着率・編成割当は保持する。
		/// </summary>
		private void ResetWeeklyLineState()
		{
			foreach (Line line in lines)
			{
				line.passengersLastWeek = 0;
				line.incomeLastWeek = 0;
				line.outlayLastWeek = 0;
				line.kamotsuNumLastWeek = 0;
				line.isOverCapacity = false;
			}
		}

		/// <summary>
		/// 直接接続と乗り継ぎの旅客数を計算する。
		/// </summary>
		private void CalculateWeeklyPassengers()
		{
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
		}

		/// <summary>
		/// 旅客数に難易度と季節の補正を適用する。
		/// </summary>
		private void ApplyWeeklyPassengerAdjustments()
		{
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
		}

		/// <summary>
		/// 今週の貨物本数を経路ごとに加算する。
		/// </summary>
		private void AccumulateWeeklyFreight()
		{
			//貨物本数計算
			foreach (Longway longway in longwayList)
			{
				//未敷設区間あれば貨物が生じないのでスキップ
				if (longway.route.Any(line => line.IsExist == false)) { continue; }

				int kamotsu = longway.CalcKamotuTrips(this) * CalcEconomicIndex() / 100;

				longway.route.ForEach(line => line.kamotsuNumLastWeek += kamotsu);
			}
		}

		/// <summary>
		/// 定着率を調整して旅客収入を計算する。
		/// </summary>
		private void UpdateWeeklyPassengerIncome()
		{
			//定着率調整
			lines.ForEach(line => line.AdjustRetentionRate());

			//旅客収入計算
			foreach (Line line in lines)
			{
				line.CalcAndReflectIncome(this);
			}
		}

		/// <summary>
		/// 平時・戦時の輸送量制限を適用する。
		/// </summary>
		private void AdjustWeeklyTransportCapacity()
		{
			//貨物便と旅客便の本数調整
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
						if (line.runningPerDay < 0)
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
		}

		/// <summary>
		/// 貨物収入と路線支出を資金・週収支・累計収支に反映する。
		/// </summary>
		private void SettleWeeklyRailwayAccounts(int kamotsuTanka)
		{
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
		}

		/// <summary>
		/// 日時を進める前の年を基準に政府補助金を反映する。
		/// </summary>
		private void ApplyWeeklySubsidy()
		{
			//政府補助金
			if (HojoStartYear <= Year && Year <= HojoEndYear && HojoAmount != 0)
			{
				Money += HojoAmount;
				if (HojoAmount < 0) { outlay -= HojoAmount; } else { income += HojoAmount; }
			}
		}

		/// <summary>不可部門の今後の投資を停止し、既存の開発停止を含む額の変更を通知する。</summary>
		private void StopUnavailableTechnologyInvestments(InvestmentAmount before)
		{
			if (!CanSteamDevelop()) { weeklyInvestment.steam = InvestmentAmountEnum.Nothing; }
			if (!CanElectricMotorDevelop()) { weeklyInvestment.electricMotor = InvestmentAmountEnum.Nothing; }
			if (!CanDieselDevelop()) { weeklyInvestment.diesel = InvestmentAmountEnum.Nothing; }
			if (!CanLinearMotorDevelop()) { weeklyInvestment.linearMotor = InvestmentAmountLinearEnum.Nothing; }
			if (!CanNewPlanDevelop()) { weeklyInvestment.newPlan = InvestmentAmountEnum.Nothing; }
			NotifyInvestmentChanged(before);
		}

		/// <summary>
		/// 技術投資を支払い、累計投資額と変更通知を更新する。
		/// </summary>
		private void ChargeWeeklyTechnologyInvestments()
		{
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
		}

		/// <summary>
		/// 動力の開発判定を順に実行し、完成イベントを追加する。
		/// </summary>
		private void CompleteWeeklyEngineDevelopment(List<GameEvent> events)
		{
			//蒸気
			if (AccumulatedInvest.steam > Math.Pow(genkaiJoki - 30, 3) / 5 * TechCost / 4)
			{
				var previousLevel = genkaiJoki;
				genkaiJoki += 5;
				events.Add(new EngineDevelopedEvent(PowerEnum.Steam, EngineDevelopmentKind.SpeedImproved, previousLevel, genkaiJoki));
				if (genkaiJoki == 150) { weeklyInvestment.steam = InvestmentAmountEnum.Nothing; }
			}

			//電車
			if (AccumulatedInvest.electricMotor > Math.Pow(genkaiDenki + 10, 3) / 10 * TechCost / 2)
			{
				if (AccumulatedInvest.electricMotor > (3000 * TechCost) && genkaiDenki == 0)
				{
					var previousLevel = genkaiDenki;
					genkaiDenki = 60;
					events.Add(new EngineDevelopedEvent(PowerEnum.Electricity, EngineDevelopmentKind.Available, previousLevel, genkaiDenki));
				}
				else if (0 < genkaiDenki && genkaiDenki < 360)
				{
					var previousLevel = genkaiDenki;
					genkaiDenki += 5;
					events.Add(new EngineDevelopedEvent(PowerEnum.Electricity, EngineDevelopmentKind.SpeedImproved, previousLevel, genkaiDenki));
				}
				if (genkaiDenki == 360) { weeklyInvestment.electricMotor = InvestmentAmountEnum.Nothing; }
			}
			if (genkaiDenki >= 360 && AccumulatedInvest.electricMotor > (50000 * genkaiDenki + 30377125) / 10 * TechCost / 2)
			{
				var previousLevel = genkaiDenki;
				genkaiDenki += 10;
				events.Add(new EngineDevelopedEvent(PowerEnum.Electricity, EngineDevelopmentKind.CostReduced, previousLevel, genkaiDenki));
			}
			if (genkaiDenki == 990) { weeklyInvestment.electricMotor = InvestmentAmountEnum.Nothing; }

			//ディーゼル
			// 通常開発の前提。高度改良の累計判定には電気技術を追加しない。
			if (genkaiDenki >= 80 && AccumulatedInvest.diesel > Math.Pow(genkaiKidosha + 30, 3) / 10 * TechCost)
			{
				if (AccumulatedInvest.diesel > (3000 * TechCost) && genkaiKidosha == 0)
				{
					var previousLevel = genkaiKidosha;
					genkaiKidosha = 40;
					events.Add(new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.Available, previousLevel, genkaiKidosha));
				}
				else if (0 < genkaiKidosha)
				{
					var previousLevel = genkaiKidosha;
					genkaiKidosha += 5;
					events.Add(new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.SpeedImproved, previousLevel, genkaiKidosha));
				}
				if (genkaiKidosha == 360) { weeklyInvestment.diesel = InvestmentAmountEnum.Nothing; }
			}
			if (genkaiKidosha >= 360 && AccumulatedInvest.diesel > (100000 * genkaiKidosha + 82638000) / 20 * TechCost)
			{
				var previousLevel = genkaiKidosha;
				genkaiKidosha += 10;
				events.Add(new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.CostReduced, previousLevel, genkaiKidosha));
			}
			if (genkaiKidosha == 990) { weeklyInvestment.diesel = InvestmentAmountEnum.Nothing; }

			//リニア
			if (AccumulatedInvest.linearMotor > (375000 * TechCost) && genkaiLinear == 0)
			{
				var previousLevel = genkaiLinear;
				genkaiLinear = 300;
				events.Add(new EngineDevelopedEvent(PowerEnum.LinearMotor, EngineDevelopmentKind.Available, previousLevel, genkaiLinear));
			}
			else if (0 < genkaiLinear && AccumulatedInvest.linearMotor > Math.Pow(genkaiLinear - 100, 3) / 20 * TechCost)
			{
				var previousLevel = genkaiLinear;
				genkaiLinear += 10;
				events.Add(new EngineDevelopedEvent(PowerEnum.LinearMotor, EngineDevelopmentKind.SpeedImproved, previousLevel, genkaiLinear));
			}
			if (genkaiLinear == 990) { weeklyInvestment.linearMotor = InvestmentAmountLinearEnum.Nothing; }
		}

		/// <summary>
		/// 新企画技術の開発判定を実行する。
		/// </summary>
		private void CompleteWeeklySpecialTechnologyDevelopment(List<GameEvent> events)
		{
			//新企画
			if (AccumulatedInvest.newPlan >= 1000 * TechCost && !isDevelopedBlockingSignal)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.BlockingSignal, genkaikyoyo, genkaikyoyo + 5));
				genkaikyoyo += 5;
				isDevelopedBlockingSignal = true;
			}
			if (AccumulatedInvest.newPlan >= 5000 * TechCost && !isDevelopedConvertibleCross)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.ConvertibleCross, genkaikyoyo, genkaikyoyo));
				isDevelopedConvertibleCross = true;
			}
			if (AccumulatedInvest.newPlan >= 10000 * TechCost && !isDevelopedAutoGate)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.AutoGate, genkaikyoyo, genkaikyoyo));
				isDevelopedAutoGate = true;
			}
			if (AccumulatedInvest.newPlan >= 20000 * TechCost && !isDevelopedCarTiltPendulum)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.CarTiltPendulum, genkaikyoyo, genkaikyoyo));
				isDevelopedCarTiltPendulum = true;
			}
			if (AccumulatedInvest.newPlan >= 30000 * TechCost && !isDevelopedRichCross)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.RichCross, genkaikyoyo, genkaikyoyo));
				isDevelopedRichCross = true;
			}
			if (AccumulatedInvest.newPlan >= 50000 * TechCost && !isDevelopedRetructableLong)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.RetructableLong, genkaikyoyo, genkaikyoyo));
				isDevelopedRetructableLong = true;
			}
			if (AccumulatedInvest.newPlan >= 200000 * TechCost && !isDevelopedDualSeat)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.DualSeat, genkaikyoyo, genkaikyoyo));
				isDevelopedDualSeat = true;
			}
			if (AccumulatedInvest.newPlan >= 300000 * TechCost && !isDevelopedMachineTilt)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.MachineTilt, genkaikyoyo, genkaikyoyo));
				isDevelopedMachineTilt = true;
			}
			if (AccumulatedInvest.newPlan >= 500000 * TechCost && !isDevelopedFreeGauge)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.FreeGauge, genkaikyoyo, genkaikyoyo));
				isDevelopedFreeGauge = true;
			}
			if (AccumulatedInvest.newPlan >= 800000 * TechCost && !isDevelopedDynamicSignal)
			{
				events.Add(new SpecialTechnologyDevelopedEvent(SpecialTechnology.DynamicSignal, genkaikyoyo, genkaikyoyo + 5));
				isDevelopedDynamicSignal = true;
				genkaikyoyo += 5;
				weeklyInvestment.newPlan = InvestmentAmountEnum.Nothing;
			}
		}

		/// <summary>
		/// 週・月・年を進め、年越し時に年次処理を実行する。
		/// </summary>
		private void AdvanceWeeklyCalendar(List<GameEvent> events)
		{
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
				// 年次処理が途中で失敗しても、期限に達した部門の停止は残る。
				StopUnavailableTechnologyInvestments(weeklyInvestment);
				events.AddRange(NextYear());

			}
		}

		/// <summary>
		/// 経済動向の位相を一週間進める。
		/// </summary>
		private void AdvanceWeeklyEconomy()
		{
			//経済動向変化
			economyTrends[0] += 360.0 / (4 * 40); //40ヶ月周期
			economyTrends[1] += 360.0 / (4 * 12 * 10); //10年周期
			economyTrends[2] += 360.0 / (4 * 12 * 20); //20年周期
			economyTrends[3] += 360.0 / (4 * 12 * 60); //60年周期
		}

		/// <summary>
		/// 週次処理後に目標を判定し、達成時はフリーモードへ移行する。
		/// </summary>
		private void CheckWeeklyGoals(List<GameEvent> events)
		{
			//目標達成状況確認
			bool goalStatus = CheckAchievement();
			if (goalStatus)
			{
				events.Add(new GoalsAchievedEvent(MYear, BasicYear + 420));
				//目標リセット
				SelectedMode.goalLineMake = null;
				SelectedMode.goalTechDevelop = new Dictionary<PowerEnum, int>();
				SelectedMode.goalLineBestSpeed = (null, 0);
				SelectedMode.goalLineManage = null;
				SelectedMode.goalMoney = null;
				MYear = BasicYear + 420;
			}
		}
	}
}
