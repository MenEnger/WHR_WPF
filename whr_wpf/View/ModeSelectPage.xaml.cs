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
			//GameInfo.InstanceInReady.Mode = ModeEnum.Free;
			gameInfo.Mode = ModeEnum.Free;


			try
			{
				gameInfo.LoadFile();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
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
