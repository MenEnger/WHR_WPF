using System.Windows;
using whr_wpf.Model;
using whr_wpf.ViewModel.Vehicle;

namespace whr_wpf.View.Vehicle
{
	/// <summary>
	/// VehicleActionListWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class VehicleActionListWindow : Window
	{
		public VehicleActionListWindow(GameInfo gameInfo)
		{
			InitializeComponent();
			DataContext = new VehicleActionListViewModel(gameInfo, this);
		}
	}
}
