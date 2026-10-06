using System;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using whr_wpf.Model;
using whr_wpf.Util;
using whr_wpf.View;

namespace whr_wpf
{
	/// <summary>
	/// MainMenuPage.xaml の相互作用ロジック
	/// </summary>
	public partial class MainMenuPage : Page
	{
		public MainMenuPage()
		{
			InitializeComponent();

			Loaded += MainMenuPage_Loaded;
		}

		private void MainMenuPage_Loaded(object sender, RoutedEventArgs e)
		{
			SetVersionInfo();
		}

		private void SetVersionInfo()
		{
			//自分自身のAssemblyを取得
			Assembly asm = Assembly.GetExecutingAssembly();
			//バージョンの取得
			System.Version ver = asm.GetName().Version;
			Version.Content = "ver." + ver.ToString();
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			ApplicationUtil.ForceExit();
		}

		private void MenuExit_Click(object sender, RoutedEventArgs e)
		{
			ApplicationUtil.ForceExit();
		}

		private void NewStart_Click(object sender, RoutedEventArgs e)
		{
			ScenarioPresentation presentation;
			try
			{
				// 設定検証・地図確保・CSV解析を従来の順序で実行する。
				presentation = ScenarioPresentation.LoadScenario(Path.Combine(AppContext.BaseDirectory, "jnr"));
			}
			catch (ScenarioValidationException ex)
			{
				MessageBox.Show(ScenarioPresentation.FormatValidationError(ex));
				// 不正な版・基準年で終了する従来のUI方針は表示側で維持する。
				ApplicationUtil.ForceExit();
				return;
			}
			catch (ScenarioReadException ex)
			{
				Console.Error.WriteLine(ex);
				MessageBox.Show(ScenarioPresentation.FormatReadError(ex), "シナリオ読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				MessageBox.Show(ScenarioPresentation.FormatUnexpectedReadError(), "シナリオ読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Pageインスタンスを渡して遷移
			var page = new DifficultyLevelSelectPage(presentation);
			NavigationService.Navigate(page);
		}

		private void ContinueStart_Click(object sender, RoutedEventArgs e)
		{
			GameInfo info = (GameInfo)ApplicationUtil.LoadData();
			if (info == null) { return; }
			var page = new GamePage(info);
			NavigationService.Navigate(page);
		}
	}
}
