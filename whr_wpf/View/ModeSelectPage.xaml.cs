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
			Loaded += InitializeButtons;
		}

		private void InitializeButtons(object sender, RoutedEventArgs e)
		{
			switch (gameInfo.Modes.Count)
			{
				default: // 7以上Modeを指定しても6つまで表示されるようにしておく
				case 6:
					mode5.Content = gameInfo.Modes[5].Name; mode5.IsEnabled = true;
					goto case 5;
				case 5:
					mode4.Content = gameInfo.Modes[4].Name; mode4.IsEnabled = true;
					goto case 4;
				case 4:
					mode3.Content = gameInfo.Modes[3].Name; mode3.IsEnabled = true;
					goto case 3;
				case 3:
					mode2.Content = gameInfo.Modes[2].Name; mode2.IsEnabled = true;
					goto case 2;
				case 2:
					mode1.Content = gameInfo.Modes[1].Name; mode1.IsEnabled = true;
					goto case 1;
				case 1:
					mode0.Content = gameInfo.Modes[0].Name; mode0.IsEnabled = true;
					break;
				case 0:
					const string Message = "#modeを指定してください";
					MessageBox.Show(Message);
					throw new ArgumentException(Message);
			}
		}

		private void MenuExit_Click(object sender, RoutedEventArgs e)
		{
			ApplicationUtil.ForceExit();
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			var page = new MainMenuPage();
			NavigationService.Navigate(page);
		}

		private void mode0_Click(object sender, RoutedEventArgs e)
		{
			NavigateGame(gameInfo.Modes[0]);
		}

		private void mode1_Click(object sender, RoutedEventArgs e)
		{
			NavigateGame(gameInfo.Modes[1]);
		}

		private void mode2_Click(object sender, RoutedEventArgs e)
		{
			NavigateGame(gameInfo.Modes[2]);
		}

		private void mode3_Click(object sender, RoutedEventArgs e)
		{
			NavigateGame(gameInfo.Modes[3]);
		}

		private void mode4_Click(object sender, RoutedEventArgs e)
		{
			NavigateGame(gameInfo.Modes[4]);
		}

		private void mode5_Click(object sender, RoutedEventArgs e)
		{
			NavigateGame(gameInfo.Modes[5]);
		}

		private void NavigateGame(Mode mode)
		{
			gameInfo.SelectedMode = mode;

			var page = new GamePage(gameInfo);
			NavigationService.Navigate(page);
		}
	}
}
