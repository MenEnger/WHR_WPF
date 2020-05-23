using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View.Line;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// 路線改造VM
	/// </summary>
	public class ReformViewModel : ViewModelBase
	{
		public ICommand SpeedUp { get; set; }
		public ICommand Hidenka { get; set; }
		public ICommand Denka { get; set; }
		public ICommand Narrow { get; set; }
		public ICommand Expanse { get; set; }
		public ICommand Addition { get; set; }
		public ICommand Remove { get; set; }
		public ICommand Taihisen { get; set; }
		public ICommand Cancel { get; set; }
		public Line Line { get; set; }
		private GameInfo gameInfo;

		public ReformViewModel(ReformWindow window, Line line, GameInfo gameInfo)
		{
			this.window = window;
			this.Line = line;
			this.gameInfo = gameInfo;

			this.SpeedUp = new SpeedUpCommand(this);
			this.Hidenka = new HidenkaCommand(this);
			this.Denka = new DenkaCommand(this);
			this.Narrow = new NarrowCommand(this);
			this.Expanse = new ExpanseCommand(this);
			this.Addition = new AdditionCommand(this);
			this.Remove = new RemoveCommand(this);
			this.Taihisen = new TaihisenCommand(this);
			this.Cancel = new CancelCommand(this);

		}

		private bool CanSpeedUp() => Line.CanSpeedUp();

		private long CalcUppedSpeed() => Line.GetSpeedUppedBestSpeed();

		public long CalcSpeedUpCost() => Line.CalcSpeedUpCost(gameInfo);

		public void ExecuteSpeedUp() => Line.SpeedUp(gameInfo);

		private bool CanHidenka()
		{
			return Line.CanUnElectrify();
		}

		public void ExecuteHidenka()
		{
			Line.UnElectrify(gameInfo);
		}

		private bool CanDenka()
		{
			return Line.CanElectrify();
		}

		private long CalcDenkaCost()
		{
			return Line.CalcElectrifyCost(gameInfo);
		}

		public void ExecuteDenka()
		{
			Line.Electrify(gameInfo);
		}

		private bool CanNarrow()
		{
			return Line.CanNarrowGauge();
		}

		private long CalcNarrowCost()
		{
			return Line.CalcNarrowGaugeCost(gameInfo);
		}

		public void ExecuteNarrow()
		{
			Line.NarrowGauge(gameInfo);
		}

		private bool CanExpanse()
		{
			return Line.CanExpanseGauge();
		}

		private long CalcExpanseCost()
		{
			return Line.CalcExpanseGaugeCost(gameInfo);
		}

		private void ExecuteExpanse()
		{
			Line.ExpanseGauge(gameInfo);
		}

		private bool CanAddLane()
		{
			return Line.CanAddLane();
		}

		private long CalcAddLaneCost()
		{
			return Line.CalcAddLaneCost(gameInfo);
		}

		private void ExecuteAddLane()
		{
			Line.AddLane(gameInfo);
		}

		private bool CanReduceOrRemoveLane()
		{
			return Line.CanReduceOrRemoveLane();
		}

		private bool IsReduceOrRemoveLane()
		{
			return Line.IsReduceOrRemoveLane();
		}

		private void ExecuteRemoveLane()
		{
			Line.ReduceOrRemoveLane(gameInfo);
		}

		private bool CanTaihiChange()
		{
			return Line.CanTaihiChange();
		}

		private void ShowTaihiChangeWindow()
		{
			var taihisenChangeWindow = new TaihisenChangeWindow(Line, gameInfo);
			taihisenChangeWindow.ShowDialog();
		}

		private void Close()
		{
			window.Close();
		}


		/// <summary>
		/// スピードアップ
		/// </summary>
		public class SpeedUpCommand : CommandBase
		{
			private ReformViewModel vm;

			public SpeedUpCommand(ReformViewModel viewModel) => vm = viewModel;

			public override bool CanExecute(object parameter) => vm.CanSpeedUp();

			public override void Execute(object parameter)
			{
				string text = $"最高速度{vm.CalcUppedSpeed()}km/hにアップすると{LogicUtil.AppendMoneyUnit(vm.CalcSpeedUpCost())}かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteSpeedUp);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// 非電化
		/// </summary>
		public class HidenkaCommand : CommandBase
		{
			private ReformViewModel vm;

			public HidenkaCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.CanHidenka();
			}

			public override void Execute(object parameter)
			{
				string text = $"この路線の電化設備を撤去します。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteHidenka);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// 電化
		/// </summary>
		public class DenkaCommand : CommandBase
		{
			private ReformViewModel vm;

			public DenkaCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.CanDenka();
			}

			public override void Execute(object parameter)
			{
				string text = $"この路線を電化するには{LogicUtil.AppendMoneyUnit(vm.CalcDenkaCost())}かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteDenka);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// 軌道縮小
		/// </summary>
		public class NarrowCommand : CommandBase
		{
			private ReformViewModel vm;

			public NarrowCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.CanNarrow();
			}

			public override void Execute(object parameter)
			{
				string text = $"この路線を狭軌に変更するには{LogicUtil.AppendMoneyUnit(vm.CalcNarrowCost())}かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteNarrow);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// 軌道拡大
		/// </summary>
		public class ExpanseCommand : CommandBase
		{
			private ReformViewModel vm;

			public ExpanseCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.CanExpanse();
			}

			public override void Execute(object parameter)
			{
				string text = $"この路線を標準軌に変更するには{LogicUtil.AppendMoneyUnit(vm.CalcExpanseCost())}かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteExpanse);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// 路線増設
		/// </summary>
		public class AdditionCommand : CommandBase
		{
			private ReformViewModel vm;

			public AdditionCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.CanAddLane();
			}

			public override void Execute(object parameter)
			{
				string text = $"この路線を増設するには{LogicUtil.AppendMoneyUnit(vm.CalcAddLaneCost())}かかります。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteAddLane);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// 路線撤去
		/// </summary>
		public class RemoveCommand : CommandBase
		{
			private ReformViewModel vm;

			public RemoveCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return vm.CanReduceOrRemoveLane();
			}

			public override void Execute(object parameter)
			{
				string text = vm.IsReduceOrRemoveLane() ? $"この路線を削減します。よろしいですか？" : "この路線を廃止します。よろしいですか？";
				ExecuteDelegete exec = new ExecuteDelegete(vm.ExecuteRemoveLane);
				vm.ExecuteWithMoney(text, exec);
			}
		}

		/// <summary>
		/// 待避線変更
		/// </summary>
		public class TaihisenCommand : CommandBase
		{
			private ReformViewModel vm;

			public TaihisenCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter) => vm.CanTaihiChange();

			public override void Execute(object parameter) => vm.ShowTaihiChangeWindow();
		}

		/// <summary>
		/// キャンセル
		/// </summary>
		public class CancelCommand : CommandBase
		{
			private ReformViewModel vm;

			public CancelCommand(ReformViewModel viewModel)
			{
				vm = viewModel;
			}

			public override bool CanExecute(object parameter)
			{
				return true;
			}

			public override void Execute(object parameter)
			{
				vm.Close();
			}
		}


	}
}
