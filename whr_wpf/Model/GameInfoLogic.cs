using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
			var resultMsgList = new List<string>();

			// 前週の状態への依存があるため、各処理と通知の順序を維持する。
			CalculateWeeklyPassengers();
			ApplyWeeklyPassengerAdjustments();
			AccumulateWeeklyFreight();
			UpdateWeeklyPassengerIncome();
			int kamotsuTanka = new Random().Next(35, 45);
			AdjustWeeklyTransportCapacity();
			SettleWeeklyRailwayAccounts(kamotsuTanka);
			ApplyWeeklySubsidy();
			ChargeWeeklyTechnologyInvestments();
			CompleteWeeklyEngineDevelopment(resultMsgList);
			CompleteWeeklySpecialTechnologyDevelopment(resultMsgList);
			AdvanceWeeklyCalendar(resultMsgList);
			AdvanceWeeklyEconomy();
			CheckWeeklyGoals(resultMsgList);

			return resultMsgList;
		}

		/// <summary>
		/// 年次処理
		/// </summary>
		private List<string> NextYear()
		{
			List<string> resultMsgList = new List<string>();

			if (Year == SteamYear) { resultMsgList.Add("今年から、蒸気機関車の設定が不可能になります。\n（現在設定中のものは引き続き使用可能です）"); }

			UpdatePopulation();

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
		/// 目標達成状況のチェック
		/// </summary>
		/// <returns>true:目標達成 false:目標未達成</returns>
		/// <exception cref="GameOverException">ゲームオーバー</exception>
		private bool CheckAchievement()
		{

			if ((Year > SelectedMode.MYear) && (SelectedMode.MYear > 0))
			{
				throw new GameOverException("目標の達成に失敗しました。ゲームオーバーです。");
			}

			bool HasGoal = false;

			//路線作成目標
			if (SelectedMode.goalLineMake.HasValue)
			{
				LineGoalTargetEnum lineGoalTarget = SelectedMode.goalLineMake.Value;
				if (!ExtractLinesByTargetType(lineGoalTarget).All(line => line.IsExist)) { return false; }
				HasGoal = true;
			}

			//技術開発目標
			foreach (var m in SelectedMode.goalTechDevelop)
			{
				switch (m.Key)
				{
					case PowerEnum.Steam:
						if (m.Value == 0 && genkaiJoki == 0) return false;
						else if (genkaiJoki < m.Value) return false;
						break;
					case PowerEnum.Electricity:
						if (m.Value == 0 && genkaiDenki == 0) return false;
						else if (genkaiDenki < m.Value) return false;
						break;
					case PowerEnum.Diesel:
						if (m.Value == 0 && genkaiKidosha == 0) return false;
						else if (genkaiKidosha < m.Value) return false;
						break;
					case PowerEnum.LinearMotor:
						if (m.Value == 0 && genkaiLinear == 0) return false;
						else if (genkaiLinear < m.Value) return false;
						break;
				}
				HasGoal = true;
			}

			//路線速度目標
			if (SelectedMode.goalLineBestSpeed.Item1.HasValue)
			{
				(LineGoalTargetEnum?, int) goalLineBestSpeed = SelectedMode.goalLineBestSpeed;
				LineGoalTargetEnum target = goalLineBestSpeed.Item1.Value;
				if (!ExtractLinesByTargetType(target).All(line =>
					line.TotalNumberTrips(false) > 0 && line.CalcHyokaSpeed() >= goalLineBestSpeed.Item2))
				{
					return false;
				}
				HasGoal = true;
			}

			//路線収支目標
			if (SelectedMode.goalLineManage.HasValue)
			{
				LineGoalTargetEnum target = SelectedMode.goalLineManage.Value;
				if (!ExtractLinesByTargetType(target).All(line => line.incomeLastWeek - line.outlayLastWeek > 0))
				{
					return false;
				}
				HasGoal = true;
			}

			//所持金目標
			if (SelectedMode.goalMoney.HasValue)
			{
				long goal = SelectedMode.goalMoney.Value;
				if (Money < goal) { return false; }
				HasGoal = true;
			}

			//最後まで目標チェックをパスしたとき、目標があれば目標達成、目標が無ければ目標未達成扱い
			return HasGoal;
		}

		/// <summary>
		/// 路線目標の対象の種類に応じて路線を抽出
		/// </summary>
		/// <param name="lineGoalTarget"></param>
		/// <returns></returns>
		private IEnumerable<Line> ExtractLinesByTargetType(LineGoalTargetEnum lineGoalTarget)
		{
			IEnumerable<Line> target = lines;
			switch (lineGoalTarget)
			{
				case LineGoalTargetEnum.MostImportant:
					target = lines.Where(line => line.grade == LineGrade.MostImportant);
					break;
				case LineGoalTargetEnum.MostImportantAndMain:
					target = lines.Where(line => line.grade == LineGrade.MostImportant || line.grade == LineGrade.Main);
					break;
				case LineGoalTargetEnum.All:
					target = lines;
					break;
			}

			return target;
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
