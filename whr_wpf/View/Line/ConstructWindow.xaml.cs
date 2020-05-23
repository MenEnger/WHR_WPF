using System.Windows;
using whr_wpf.Model;
using whr_wpf.ViewModel;

namespace whr_wpf
{
	/// <summary>
	/// ConstructWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class ConstructWindow : Window
	{

		public ConstructWindow(Line line, GameInfo gameInfo)
		{
			InitializeComponent();
			var vm = new ConstructWindowViewModel(line, gameInfo, this);
			this.DataContext = vm;
			//Loaded += (s, e) => { Init(); };
		}

		private void Init()
		{
			this.LaneSu.SelectedItem = ConstructWindowViewModel.LaneSuList[1];
		}


	}
}
