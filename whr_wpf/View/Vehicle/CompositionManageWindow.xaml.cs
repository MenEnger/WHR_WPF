using System.Windows;
using whr_wpf.ViewModel.Vehicle;

namespace whr_wpf.View.Vehicle
{
	/// <summary>
	/// CompositionManageWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class CompositionManageWindow : Window
	{
		public CompositionManageWindow(Model.GameInfo gameInfo)
		{
			InitializeComponent();
			DataContext = new CompositionManageViewModel(gameInfo, this);
		}
	}
}
