using System.Windows;
using whr_wpf.Model;
using whr_wpf.ViewModel.Info;

namespace whr_wpf.View.Info
{
	/// <summary>
	/// InfoWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class InfoWindow : Window
	{
		public InfoWindow(GameInfo gameInfo)
		{
			InitializeComponent();
			this.DataContext = new InfoViewModel(gameInfo, this);
		}
	}
}
