using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using whr_wpf.Util;

namespace whr_wpf.Model
{
	public partial class GameInfo : INotifyPropertyChanged
	{
		/// <summary>蒸気機関の週次投資額を設定する。</summary>
		public void SetSteamInvestment(InvestmentAmountEnum amount)
		{
			ValidateInvestmentSetting(TechnologyInvestmentDepartment.Steam, (int)amount, CanSteamDevelop());
			var before = weeklyInvestment;
			weeklyInvestment.steam = amount;
			NotifyInvestmentChanged(before);
		}

		/// <summary>電気モーターの週次投資額を設定する。</summary>
		public void SetElectricInvestment(InvestmentAmountEnum amount)
		{
			ValidateInvestmentSetting(TechnologyInvestmentDepartment.Electric, (int)amount, CanElectricMotorDevelop());
			var before = weeklyInvestment;
			weeklyInvestment.electricMotor = amount;
			NotifyInvestmentChanged(before);
		}

		/// <summary>ディーゼルの週次投資額を設定する。</summary>
		public void SetDieselInvestment(InvestmentAmountEnum amount)
		{
			ValidateInvestmentSetting(TechnologyInvestmentDepartment.Diesel, (int)amount, CanDieselDevelop());
			var before = weeklyInvestment;
			weeklyInvestment.diesel = amount;
			NotifyInvestmentChanged(before);
		}

		/// <summary>リニアモーターの週次投資額を設定する。</summary>
		public void SetLinearInvestment(InvestmentAmountLinearEnum amount)
		{
			ValidateInvestmentSetting(TechnologyInvestmentDepartment.Linear, (int)amount, CanLinearMotorDevelop());
			var before = weeklyInvestment;
			weeklyInvestment.linearMotor = amount;
			NotifyInvestmentChanged(before);
		}

		/// <summary>新企画の週次投資額を設定する。</summary>
		public void SetNewPlanInvestment(InvestmentAmountEnum amount)
		{
			ValidateInvestmentSetting(TechnologyInvestmentDepartment.NewPlan, (int)amount, CanNewPlanDevelop());
			var before = weeklyInvestment;
			weeklyInvestment.newPlan = amount;
			NotifyInvestmentChanged(before);
		}

		private static void ValidateInvestmentSetting(TechnologyInvestmentDepartment department, int amount, bool canInvest)
		{
			// 不可部門でも、呼出元から投資を止めることは許可する。
			if (amount != 0 && !canInvest)
			{
				throw new TechnologyInvestmentRejectedException(new TechnologyInvestmentFailure(department, amount));
			}
		}

		private void NotifyInvestmentChanged(InvestmentAmount before)
		{
			if (!before.Equals(weeklyInvestment)) { OnPropertyChanged(nameof(weeklyInvestment)); }
		}

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
		/// <returns>開発可否と判定時点の値</returns>
		public VehicleCreationCheck CheckCreateVehicle(string Name, int BestSpeed, PowerEnum Power, CarGaugeEnum Gauge, SeatEnum Seat, CarTiltEnum tilt)
		{
			VehicleCreationCheck Result(VehicleCreationReason reason, int speedLimit = 0) =>
				new(reason, Power, BestSpeed, speedLimit, Gauge, Seat, tilt, Year, SteamYear);

			if (string.IsNullOrWhiteSpace(Name)) { return Result(VehicleCreationReason.MissingName); }

			if (Power == PowerEnum.LinearMotor)
			{
				if (genkaiLinear == 0) { return Result(VehicleCreationReason.EngineUnavailable); }
				else if (BestSpeed > genkaiLinear) { return Result(VehicleCreationReason.SpeedExceeded, genkaiLinear); }
			}

			if (Gauge == CarGaugeEnum.FreeGauge && !isDevelopedFreeGauge) { return Result(VehicleCreationReason.FreeGaugeUnavailable); }

			if (Power == PowerEnum.Steam)
			{
				if (!IsSteamAvailable()) { return Result(VehicleCreationReason.SteamExpired); }
				if (BestSpeed > genkaiJoki) { return Result(VehicleCreationReason.SpeedExceeded, genkaiJoki); }
			}

			if (Power == PowerEnum.Electricity)
			{
				if (genkaiDenki == 0) { return Result(VehicleCreationReason.EngineUnavailable); }
				else if (BestSpeed > genkaiDenki) { return Result(VehicleCreationReason.SpeedExceeded, genkaiDenki); }
			}

			if (Power == PowerEnum.Diesel)
			{
				if (genkaiKidosha == 0) { return Result(VehicleCreationReason.EngineUnavailable); }
				else if (BestSpeed > genkaiKidosha) { return Result(VehicleCreationReason.SpeedExceeded, genkaiKidosha); }
			}

			if (Seat == SeatEnum.Dual && !isDevelopedDualSeat) { return Result(VehicleCreationReason.SeatUnavailable); }
			if (Seat == SeatEnum.Convertible && !isDevelopedConvertibleCross) { return Result(VehicleCreationReason.SeatUnavailable); }
			if (Seat == SeatEnum.RetructableLong && !isDevelopedRetructableLong) { return Result(VehicleCreationReason.SeatUnavailable); }
			if (Seat == SeatEnum.Rich && !isDevelopedRichCross) { return Result(VehicleCreationReason.SeatUnavailable); }
			if (Seat == SeatEnum.DoubleDeckerRich && !isDevelopedRichCross) { return Result(VehicleCreationReason.SeatUnavailable); }

			switch (tilt)
			{
				case CarTiltEnum.Pendulum:
					if (!isDevelopedCarTiltPendulum) { return Result(VehicleCreationReason.TiltUnavailable); }
					break;
				case CarTiltEnum.SimpleMecha:
				case CarTiltEnum.HighMecha:
					if (!isDevelopedMachineTilt) { return Result(VehicleCreationReason.TiltUnavailable); }
					break;
			}

			return Result(VehicleCreationReason.None);
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
			VehicleCreationCheck check = CheckCreateVehicle(name, bestSpeed, power, gauge, seat, tilt);

			if (!check.CanCreateVehicle) { throw new VehicleDevelopmentRejectedException(check); }

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
		/// <returns>発生時点の値を保持するイベントのリスト</returns>
		public List<GameEvent> NextWeek()
		{
			var events = new List<GameEvent>();

			// 原作と同様に週次の路線状態を初期化し、各処理と通知の順序を維持する。
			ResetWeeklyLineState();
			CalculateWeeklyPassengers();
			ApplyWeeklyPassengerAdjustments();
			AccumulateWeeklyFreight();
			UpdateWeeklyPassengerIncome();
			int kamotsuTanka = new Random().Next(35, 46);
			AdjustWeeklyTransportCapacity();
			SettleWeeklyRailwayAccounts(kamotsuTanka);
			ApplyWeeklySubsidy();
			StopUnavailableTechnologyInvestments(weeklyInvestment);
			ChargeWeeklyTechnologyInvestments();
			var investmentBeforeDevelopment = weeklyInvestment;
			CompleteWeeklyEngineDevelopment(events);
			CompleteWeeklySpecialTechnologyDevelopment(events);
			StopUnavailableTechnologyInvestments(investmentBeforeDevelopment);
			AdvanceWeeklyCalendar(events);
			AdvanceWeeklyEconomy();
			CheckWeeklyGoals(events);

			return events;
		}

		/// <summary>
		/// 年次処理
		/// </summary>
		private List<GameEvent> NextYear()
		{
			List<GameEvent> events = new List<GameEvent>();

			if (Year == SteamYear) { events.Add(new SteamAvailabilityEndedEvent(Year)); }

			UpdatePopulation();

			//戦時体制
			if (modss == null)
			{
				WarMode warMode = warModeList.FirstOrDefault(warMode => warMode.StartYear == Year);
				if (warMode != null)
				{
					modss = warMode;
					events.Add(new WarStartedEvent(warMode.StartYear, warMode.EndYear, warMode.kamotsuIndex));
				}
			}
			else
			{
				if (modss.EndYear == Year)
				{
					var endedWar = new WarEndedEvent(modss.StartYear, modss.EndYear, modss.kamotsuIndex);
					modss = null;
					events.Add(endedWar);
				}
			}

			return events;
		}

		/// <summary>
		/// 目標達成状況のチェック
		/// </summary>
		/// <returns>true:目標達成 false:目標未達成</returns>
		/// <exception cref="GameOverException">ゲームオーバー</exception>
		private bool CheckAchievement()
		{

			// 達成後はフリーモードの期限へ変わるため、初期設定ではなく実行中の期限を使う。
			if ((Year > MYear) && (MYear > 0))
			{
				throw new GameOverException(new(GameOverReason.DeadlineExceeded, Year, MYear));
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
		/// 編成を作成し、ゲームへ登録する。
		/// </summary>
		public Composition CreateComposition(string name, IEnumerable<KeyValuePair<Car, int>> vehicleNumbers)
		{
			var destination = compositions;
			Composition result = CompositionFactory.CreateComposition(name, vehicleNumbers);
			destination.Add(result);
			return result;
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
