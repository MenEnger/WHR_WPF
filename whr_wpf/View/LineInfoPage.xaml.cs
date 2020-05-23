using System;
using System.Windows.Controls;
using System.Windows.Navigation;
using whr_wpf.Model;
using whr_wpf.ViewModel;

namespace whr_wpf
{
	/// <summary>
	/// LineInfoPage.xaml の相互作用ロジック
	/// </summary>
	public partial class LineInfoPage : Page
	{
		private GameInfo gameInfo;

		public LineInfoPage(Line line, GameInfo gameInfo)
		{
			InitializeComponent();

			this.gameInfo = gameInfo;
			var vm = new LineInfoViewModel(line, this, gameInfo);
			this.DataContext = vm;
			LineList.ItemsSource = gameInfo.lines;
			LineList.SelectedItem = line;
		}

		private void LineList_DropDownClosed(object sender, EventArgs e)
		{
			if (LineList.SelectedItem is Line line)
			{
				var page = new LineInfoPage(line, gameInfo);
				NavigationService.Navigate(page);
			}
		}
	}

}
