using System.Windows;
using whr_wpf.Model;
using whr_wpf.ViewModel.Vehicle;

namespace whr_wpf.View.Vehicle
{
	/// <summary>
	/// VehicleDevelopWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class VehicleDevelopWindow : Window
	{
		public VehicleDevelopWindow(GameInfo gameInfo)
		{
			InitializeComponent();
			DataContext = new VehicleDevelopViewModel(gameInfo, this);
		}
	}
}
