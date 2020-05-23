using System.Windows;
using whr_wpf.Model;
using whr_wpf.ViewModel.Technology;

namespace whr_wpf.View.Technology
{
	/// <summary>
	/// TechnologyDevelopWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class TechnologyDevelopWindow : Window
	{
		public TechnologyDevelopWindow(GameInfo gameInfo)
		{
			InitializeComponent();
			DataContext = new TechnologyDevelopViewModel(gameInfo, this);
		}
	}
}
