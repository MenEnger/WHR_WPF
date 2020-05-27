using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using whr_wpf.Model;

namespace whr_wpf
{
	/// <summary>
	/// ModeSelectPage.xaml の相互作用ロジック
	/// </summary>
	public partial class ModeSelectPage : Page
	{
		private GameInfo gameInfo;

		public ModeSelectPage(GameInfo gameInfo)
		{
			InitializeComponent();
			this.gameInfo = gameInfo;
		}

		private void MenuExit_Click(object sender, RoutedEventArgs e)
		{
			ApplicationUtil.ForceExit();
		}

		private void Free_Click(object sender, RoutedEventArgs e)
		{
			gameInfo.Mode = ModeEnum.Free;

			var page = new GamePage(gameInfo);
			NavigationService.Navigate(page);
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			var page = new MainMenuPage();
			NavigationService.Navigate(page);
		}
	}
}
