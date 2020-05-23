using System.Windows.Controls;
using System.Windows.Navigation;

namespace whr_wpf
{
	/// <summary>
	/// ToolWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class ToolWindow : NavigationWindow
	{
		public ToolWindow(Page page)
		{
			InitializeComponent();
			Loaded += (s, e) => { NavigationService.Navigate(page); };
		}
	}
}
