using System.Windows;
using whr_wpf.Model;
using whr_wpf.ViewModel;

namespace whr_wpf.View.Line
{
	/// <summary>
	/// TaihisenChangeWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class TaihisenChangeWindow : Window
	{
		public TaihisenChangeWindow(Model.Line line, GameInfo gameInfo)
		{
			InitializeComponent();
			var vm = new TaihisenChangeViewModel(line, gameInfo, this);
			this.DataContext = vm;
		}
	}
}
