using System.Windows;
using whr_wpf.ViewModel.Vehicle;

namespace whr_wpf.View.Vehicle
{
	/// <summary>
	/// CompositionMakeWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class CompositionMakeWindow : Window
	{
		public CompositionMakeWindow(Model.GameInfo gameInfo)
		{
			InitializeComponent();
			DataContext = new CompositionMakeViewModel(gameInfo, this);
		}
	}
}
