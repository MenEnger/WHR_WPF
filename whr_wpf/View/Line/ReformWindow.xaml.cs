using System.Windows;
using whr_wpf.ViewModel;

namespace whr_wpf.View.Line
{
	/// <summary>
	/// ReformWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class ReformWindow : Window
	{
		public ReformWindow(Model.Line line, Model.GameInfo currentInstance)
		{
			InitializeComponent();
			this.DataContext = new ReformViewModel(this, line, currentInstance);
		}
	}
}
