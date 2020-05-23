using System.Windows;
using whr_wpf.Model;
using whr_wpf.ViewModel.Vehicle;

namespace whr_wpf.View.Vehicle
{
	/// <summary>
	/// VehicleInfoWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class VehicleInfoWindow : Window
	{
		public VehicleInfoWindow(GameInfo gameInfo)
		{
			InitializeComponent();
			DataContext = new VehicleInfoViewModel(gameInfo, this);
		}
	}
}
