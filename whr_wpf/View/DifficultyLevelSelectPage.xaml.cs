using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using whr_wpf.Model;

namespace whr_wpf
{
	/// <summary>
	/// DifficultyLevelSelectPage.xaml の相互作用ロジック
	/// </summary>
	public partial class DifficultyLevelSelectPage : Page
	{
		private GameInfo gameInfo;

		public DifficultyLevelSelectPage(GameInfo gameInfo)
		{
			InitializeComponent();
			this.gameInfo = gameInfo;
		}

		private void MenuExit_Click(object sender, RoutedEventArgs e)
		{
			ApplicationUtil.ForceExit();
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			// Pageインスタンスを渡して遷移
			var page = new MainMenuPage();
			NavigationService.Navigate(page);
		}

		private void VeryEasy_Click(object sender, RoutedEventArgs e)
		{
			gameInfo.Difficulty = DifficultyLevelEnum.VeryEasy;
			GotoModeSelect();
		}

		private void Easy_Click(object sender, RoutedEventArgs e)
		{
			gameInfo.Difficulty = DifficultyLevelEnum.Easy;
			GotoModeSelect();
		}

		private void Medium_Click(object sender, RoutedEventArgs e)
		{
			gameInfo.Difficulty = DifficultyLevelEnum.Normal;
			GotoModeSelect();
		}

		private void Hard_Click(object sender, RoutedEventArgs e)
		{
			gameInfo.Difficulty = DifficultyLevelEnum.Hard;
			GotoModeSelect();
		}

		private void VeryHard_Click(object sender, RoutedEventArgs e)
		{
			gameInfo.Difficulty = DifficultyLevelEnum.VeryHard;
			GotoModeSelect();
		}

		private void GotoModeSelect()
		{
			var page = new ModeSelectPage(gameInfo);
			NavigationService.Navigate(page);
		}
	}
}
