using System.Windows;
using whr_wpf.ViewModel;

namespace whr_wpf.View.Line
{
	/// <summary>
	/// KeitoDiagramSettingWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class KeitoDiagramSettingWindow : Window
	{
		public KeitoDiagramSettingWindow(Model.KeitoDiagram keitoDiagram, Model.GameInfo gameInfo)
		{
			InitializeComponent();
			var vm = new KeitoDiagramSettingViewModel(gameInfo, keitoDiagram, this);
			this.DataContext = vm;
		}
	}
}
