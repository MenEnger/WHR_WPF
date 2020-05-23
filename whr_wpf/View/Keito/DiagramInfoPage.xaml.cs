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
	public partial class DiagramInfoPage : Page
	{
		private GameInfo gameInfo;

		public DiagramInfoPage(KeitoDiagram diagram, GameInfo gameInfo)
		{
			InitializeComponent();
			var vm = new DiagramInfoViewModel(diagram, this, gameInfo);
			this.DataContext = vm;
			this.gameInfo = gameInfo;

			DiagramList.ItemsSource = gameInfo.diagrams;
			DiagramList.DisplayMemberPath = "Name";
			DiagramList.SelectedItem = diagram;
			gameInfo.lastSeenKeito = diagram;
		}

		private void DiagramList_DropDownClosed(object sender, EventArgs e)
		{
			if (DiagramList.SelectedItem is KeitoDiagram diagram)
			{
				var page = new DiagramInfoPage(diagram, gameInfo);
				NavigationService.Navigate(page);
			}
		}
	}

}
