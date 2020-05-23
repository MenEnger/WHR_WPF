using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View.Technology;

namespace whr_wpf.ViewModel.Technology
{
	/// <summary>
	/// 技術開発VM
	/// </summary>
	class TechnologyDevelopViewModel : ViewModelBase
	{
		public TechnologyDevelopViewModel(GameInfo gameInfo, TechnologyDevelopWindow window)
		{
			this.gameInfo = gameInfo;
			this.window = window;

			listener = new PropertyChangedEventListener(gameInfo, PropertyChangedInGameInfo);
			window.Closed += (sender, eventArgs) => { listener.Dispose(); };

			this.Cancel = new CancelCommand(this);
		}

		private IDisposable listener;

		private void PropertyChangedInGameInfo(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(GameInfo.AccumulatedInvest):
					InvokeAllNotify();
					break;
			}
		}

		/// <summary>
		/// 投資額リスト
		/// </summary>
		public static ImmutableDictionary<InvestmentAmountEnum, string> InvestAmountList =>
			typeof(InvestmentAmountEnum)
			.GetEnumValues()
			.Cast<InvestmentAmountEnum>()
			.ToImmutableDictionary(investEnum => investEnum, investEnum => investEnum.ToName());

		/// <summary>
		/// リニア投資額リスト
		/// </summary>
		public static ImmutableDictionary<InvestmentAmountLinearEnum, string> InvestAmountLinearList =>
			typeof(InvestmentAmountLinearEnum)
			.GetEnumValues()
			.Cast<InvestmentAmountLinearEnum>()
			.ToImmutableDictionary(investEnum => investEnum, investEnum => investEnum.ToName());

		private GameInfo gameInfo;
		public ICommand Cancel { get; set; }


		/// <summary>
		/// 蒸気機関週次投資額
		/// </summary>
		public InvestmentAmountEnum SteamInvest
		{
			get => gameInfo.weeklyInvestment.steam; set
			{
				gameInfo.weeklyInvestment.steam = value;
			}
		}

		/// <summary>
		/// 蒸気機関に投資可能か
		/// </summary>
		public bool CanSteamInvest => gameInfo.CanSteamDevelop();

		/// <summary>
		/// 蒸気機関累計投資額
		/// </summary>
		public string SteamAccumuInvestment => LogicUtil.AppendMoneyUnit(gameInfo.AccumulatedInvest.steam);


		/// <summary>
		/// 電気モーター週次投資額
		/// </summary>
		public InvestmentAmountEnum ElectricInvest
		{
			get => gameInfo.weeklyInvestment.electricMotor; set
			{
				gameInfo.weeklyInvestment.electricMotor = value;
			}
		}

		/// <summary>
		/// 電気モーターに投資可能か
		/// </summary>
		public bool CanElectricInvest => gameInfo.CanElectricMotorDevelop();

		/// <summary>
		/// 電気モーター累計投資額
		/// </summary>
		public string ElectricAccumuInvestment => LogicUtil.AppendMoneyUnit(gameInfo.AccumulatedInvest.electricMotor);


		/// <summary>
		/// ディーゼル週次投資額
		/// </summary>
		public InvestmentAmountEnum DieselInvest
		{
			get => gameInfo.weeklyInvestment.diesel; set
			{
				gameInfo.weeklyInvestment.diesel = value;
			}
		}

		/// <summary>
		/// ディーゼル投資可能か
		/// </summary>
		public bool CanDieselInvest => gameInfo.CanDieselDevelop();

		/// <summary>
		/// ディーゼル累計投資額
		/// </summary>
		public string DieselAccumuInvestment => LogicUtil.AppendMoneyUnit(gameInfo.AccumulatedInvest.diesel);


		/// <summary>
		/// リニアモーター週次投資額
		/// </summary>
		public InvestmentAmountLinearEnum LinearInvest
		{
			get => gameInfo.weeklyInvestment.linearMotor; set
			{
				gameInfo.weeklyInvestment.linearMotor = value;
			}
		}

		/// <summary>
		/// リニア投資可能か
		/// </summary>
		public bool CanLinearInvest => gameInfo.CanLinearMotorDevelop();

		/// <summary>
		/// リニアモーター累計投資額
		/// </summary>
		public string LinearAccumuInvestment => LogicUtil.AppendMoneyUnit(gameInfo.AccumulatedInvest.linearMotor);


		/// <summary>
		/// 新企画週次投資額
		/// </summary>
		public InvestmentAmountEnum NewPlanInvest
		{
			get => gameInfo.weeklyInvestment.newPlan; set
			{
				gameInfo.weeklyInvestment.newPlan = value;
			}
		}

		/// <summary>
		/// 新企画投資できるか
		/// </summary>
		public bool CanNewPlanInvest => gameInfo.CanNewPlanDevelop();

		/// <summary>
		/// 新企画累計投資額
		/// </summary>
		public string NewPlanAccumuInvestment => LogicUtil.AppendMoneyUnit(gameInfo.AccumulatedInvest.newPlan);

		/// <summary>
		/// 閉じるコマンド
		/// </summary>
		class CancelCommand : CommandBase
		{
			TechnologyDevelopViewModel vm;
			public CancelCommand(TechnologyDevelopViewModel vm) => this.vm = vm;
			public override bool CanExecute(object parameter) => true;
			public override void Execute(object parameter) => vm.window.Close();
		}
	}
}
