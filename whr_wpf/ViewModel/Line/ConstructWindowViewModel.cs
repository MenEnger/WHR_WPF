using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Input;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.ViewModel.Component;

namespace whr_wpf.ViewModel
{

	public partial class ConstructWindowViewModel : ViewModelBase
	{
		//選択肢の内容
		public static List<LaneNumViewModel> LaneSuList => new List<LaneNumViewModel>
		{
			new LaneNumViewModel { Caption = "単線", LaneSu = 1 },
			new LaneNumViewModel { Caption = "複線", LaneSu = 2 },
			new LaneNumViewModel { Caption = "複々線", LaneSu = 4 }
		};
		public static List<RailTypeViewModel> RailTypeList => new List<RailTypeViewModel>
		{
			new RailTypeViewModel
			{
				Caption = "非電化狭軌",
				IsElectrified = false,
				RailGauge = Model.RailGaugeEnum.Narrow,
				RailType = Model.RailTypeEnum.Iron
			},
			new RailTypeViewModel
			{
				Caption = "非電化標準軌",
				IsElectrified = false,
				RailGauge = Model.RailGaugeEnum.Regular,
				RailType = Model.RailTypeEnum.Iron
			},
			new RailTypeViewModel
			{
				Caption = "電化狭軌",
				IsElectrified = true,
				RailGauge = Model.RailGaugeEnum.Narrow,
				RailType = Model.RailTypeEnum.Iron
			},
			new RailTypeViewModel
			{
				Caption = "電化標準軌",
				IsElectrified = true,
				RailGauge = Model.RailGaugeEnum.Regular,
				RailType = Model.RailTypeEnum.Iron
			},
			new RailTypeViewModel
			{
				Caption = "リニアレール",
				IsElectrified = true,
				RailType = Model.RailTypeEnum.LinearMotor
			},
		};
		public static List<TaihiViewComponent> TaihiList
		{
			get
			{
				return TaihiViewComponent.CreateViewList();
			}
		}

		//プロパティ
		public ICommand SpeedUp { get; set; }
		public ICommand SpeedDown { get; set; }
		public ICommand Kettei { get; set; }
		public ICommand Cancel { get; set; }

		private GameInfo gameInfo;
		public ConstructWindow Window { get; set; }

		private int _bestSpeed = 120;
		private LaneNumViewModel laneSu;
		private RailTypeViewModel railType;
		private TaihiViewComponent taihisen;

		public int BestSpeed
		{
			get { return _bestSpeed; }
			set
			{
				if (value < 40)
				{
					MessageBox.Show("40km/h以上のスピードを指定してください");
					return;
				}
				if (railType.RailType == RailTypeEnum.Iron && railType.RailGauge == RailGaugeEnum.Narrow && value > 300)
				{
					MessageBox.Show("狭軌で出せる速度は300km/h以内です");
					return;
				}
				if (railType.RailType == RailTypeEnum.Iron && value > 360)
				{
					MessageBox.Show("軌道で出せる速度は360km/h以内です");
					return;
				}

				_bestSpeed = value;
				this.OnPropertyChanged(nameof(BestSpeed));
				this.OnPropertyChanged(nameof(EstimatedCost));
			}
		}

		public LaneNumViewModel LaneSu
		{
			get => laneSu; set
			{
				laneSu = value;
				this.OnPropertyChanged(nameof(LaneSu));
				this.OnPropertyChanged(nameof(EstimatedCost));
			}
		}
		public RailTypeViewModel RailType
		{
			get => railType;
			set
			{
				railType = value;
				this.OnPropertyChanged(nameof(RailType));
				this.OnPropertyChanged(nameof(EstimatedCost));
			}
		}
		public TaihiViewComponent Taihisen
		{
			get => taihisen; set
			{
				taihisen = value;
				this.OnPropertyChanged(nameof(Taihisen));
				this.OnPropertyChanged(nameof(EstimatedCost));
			}
		}
		public string EstimatedCost
		{
			get
			{
				return $"見積もり {LogicUtil.AppendMoneyUnit(CalcCost())}";
			}
		}

		public Line Line { get; set; }

		public long CalcCost()
		{
			if (LaneSu is null) { return 0; }
			return Line.CalcConstructCost(BestSpeed,
									LaneSu.LaneSu,
									RailType.RailType,
									RailType.RailGauge,
									RailType.IsElectrified,
									Taihisen.Enum,
									gameInfo);
		}

		public void Construct()
		{
			Line.Construct(
						BestSpeed,
						RailType.RailType,
						RailType.IsElectrified,
						RailType.RailGauge,
						LaneSu.LaneSu,
						Taihisen.Enum,
						gameInfo);
		}

		//コンストラクタ
		public ConstructWindowViewModel(Line line, GameInfo gameInfo, ConstructWindow window)
		{
			this.Window = window;
			this.Line = line;
			this.SpeedUp = new SpeedUpCommand(this);
			this.SpeedDown = new SpeedDownCommand(this);
			this.Kettei = new KetteiCommand(this);
			this.Cancel = new CancelCommand(this);
			this.gameInfo = gameInfo;

			laneSu = LaneSuList[1];
			railType = RailTypeList[2];
			taihisen = TaihiList[3];
			InvokeAllNotify();
		}

		/// <summary>
		/// 路線数を表すモデル(Combobox用)
		/// </summary>
		public class LaneNumViewModel : IEquatable<LaneNumViewModel>
		{
			public string Caption { get; set; }
			public int LaneSu { get; set; }

			public bool Equals([AllowNull] LaneNumViewModel other)
			{
				return other switch
				{
					null => false,
					_ => Caption == other.Caption && LaneSu == other.LaneSu
				};
			}
		}

		/// <summary>
		/// 路線規格を表すモデル(Combobox用)
		/// </summary>
		public class RailTypeViewModel : IEquatable<RailTypeViewModel>
		{
			public string Caption { get; set; }
			public RailTypeEnum RailType { get; set; }
			public RailGaugeEnum? RailGauge { get; set; }
			public bool IsElectrified { get; set; }

			public bool Equals([AllowNull] RailTypeViewModel other)
			{
				return other switch
				{
					null => false,
					_ => Caption == other.Caption
					&& RailType == other.RailType
					&& RailGauge == other.RailGauge
					&& IsElectrified == other.IsElectrified
				};
			}
		}

		/// <summary>
		/// 路線速度増加時コマンド
		/// </summary>
		public class SpeedUpCommand : ICommand
		{
			private ConstructWindowViewModel vm;
			private int increments = 10;

			public SpeedUpCommand(ConstructWindowViewModel viewModel)
			{
				vm = viewModel;
			}

			public event EventHandler CanExecuteChanged
			{
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}

			public bool CanExecute(object parameter)
			{
				return (vm.BestSpeed + increments) <= 990;
			}

			public void Execute(object parameter)
			{
				vm.BestSpeed += increments;
			}
		}

		/// <summary>
		/// 路線速度減少時コマンド
		/// </summary>
		public class SpeedDownCommand : ICommand
		{
			private ConstructWindowViewModel vm;
			private int diff = 10;

			public SpeedDownCommand(ConstructWindowViewModel viewModel)
			{
				vm = viewModel;
			}

			public event EventHandler CanExecuteChanged
			{
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}

			public bool CanExecute(object parameter)
			{
				return (vm.BestSpeed - diff) >= 0;
			}

			public void Execute(object parameter)
			{
				vm.BestSpeed -= diff;
			}
		}

		//決定時のコマンド
		public class KetteiCommand : ICommand
		{
			private ConstructWindowViewModel vm;

			public KetteiCommand(ConstructWindowViewModel viewModel)
			{
				vm = viewModel;
			}

			public event EventHandler CanExecuteChanged
			{
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}

			public bool CanExecute(object parameter)
			{
				return vm.LaneSu != null && vm.RailType != null && vm.Taihisen != null;
			}

			public void Execute(object parameter)
			{
				//確認
				MessageBoxResult constructConfirm = MessageBox.Show($"{LogicUtil.AppendMoneyUnit(vm.CalcCost())}の資金が必要です。建造しますか？", "路線建造", MessageBoxButton.YesNo);
				if (constructConfirm == MessageBoxResult.Yes)
				{
					try
					{
						vm.Construct();
					}
					catch (MoneyShortException)
					{
						MessageBox.Show("お金が足りません");
					}
					vm.Window.Close();

				}
			}
		}

		//キャンセル
		public class CancelCommand : ICommand
		{
			private ConstructWindowViewModel vm;

			public CancelCommand(ConstructWindowViewModel viewModel)
			{
				vm = viewModel;
			}

			public event EventHandler CanExecuteChanged
			{
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}

			public bool CanExecute(object parameter)
			{
				return true;
			}

			public void Execute(object parameter)
			{
				vm.Window.Close();
			}
		}
	}


}
