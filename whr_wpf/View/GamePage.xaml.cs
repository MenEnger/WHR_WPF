using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using whr_wpf.Model;
using whr_wpf.ViewModel;

namespace whr_wpf
{
	/// <summary>
	/// GamePage.xaml の相互作用ロジック
	/// </summary>
	public partial class GamePage : Page
	{
		private GameInfo gameInfo;

		public GamePage(GameInfo gameInfo)
		{
			InitializeComponent();

			this.gameInfo = gameInfo;
			this.DataContext = new GamePageViewModel(gameInfo, this);
			Loaded += (s, e) => { DrawMap(); };
			Loaded += ShowModeMessageAsync;
		}

		private void ShowModeMessageAsync(object sender, EventArgs e)
		{
			Task.Run(() =>
			{
				MessageBox.Show(gameInfo.SelectedMode.Message);
			});
		}

		//ゲーム画面の描画
		public void DrawMap()
		{
			MapCanvas.Children.Clear();

			var info = gameInfo;

			//地図
			MapCanvas.Background = new ImageBrush(info.map);

			////路線
			//foreach (Model.Line lineRail in info.lines)
			//{
			//	var lineShape = new System.Windows.Shapes.Line();
			//	lineShape.X1 = lineRail.start.X;
			//	lineShape.Y1 = lineRail.start.Y;
			//	lineShape.X2 = lineRail.end.X;
			//	lineShape.Y2 = lineRail.end.Y;
			//	lineShape.Stroke = Brushes.White;
			//	lineShape.StrokeThickness = 1;
			//	lineShape.MouseLeftButtonDown += (sender, e) =>
			//	{
			//		var page = new LineInfoPage(lineRail);
			//		var window = new ToolWindow(page);

			//		window.ShowDialog();
			//	};
			//	MapCanvas.Children.Add(lineShape);
			//}

			// 駅と駅名
			foreach (Station station in info.stations)
			{

				//if (station.nameShown)
				//{
				//	var slabel = new Label();
				//	slabel.Content = station.Name;
				//	slabel.Margin = new Thickness(station.X, station.Y - (int)station.Size, 0, 0);
				//	slabel.FontSize = (int)station.Size;
				//	slabel.Foreground = Brushes.White;
				//	slabel.MouseLeftButtonDown += (sender, e) =>
				//	{
				//		var vm = new TownViewModel(station);
				//		var window = new TownWindow(vm, gameInfo);
				//		window.ShowDialog();
				//	};

				//	MapCanvas.Children.Add(slabel);
				//}

				var ellipse = new Ellipse();
				ellipse.Width = (int)station.Size / 4 * 1.5;
				ellipse.Height = (int)station.Size / 4 * 1.5;
				ellipse.Margin = new Thickness(station.X, station.Y, 0, 0);
				ellipse.Stroke = Brushes.Black;
				ellipse.Fill = Brushes.White;
				ellipse.RenderTransform = new TranslateTransform(-ellipse.Width / 2, -ellipse.Height / 2);
				ellipse.MouseLeftButtonDown += (sender, e) =>
				{
					var vm = new TownViewModel(station);
					var window = new TownWindow(vm, gameInfo);
					window.ShowDialog();
				};
				MapCanvas.Children.Add(ellipse);

			}
		}
	}

}
