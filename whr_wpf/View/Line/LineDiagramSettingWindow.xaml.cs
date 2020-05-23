using System.Windows;
using whr_wpf.ViewModel;

namespace whr_wpf.View.Line
{
	/// <summary>
	/// LineDiagramSettingWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class LineDiagramSettingWindow : Window
	{
		public LineDiagramSettingWindow(Model.Line line, Model.GameInfo gameInfo)
		{
			InitializeComponent();
			var vm = new LineDiagramSettingViewModel(gameInfo, line, this);
			this.DataContext = vm;
		}
	}
}
