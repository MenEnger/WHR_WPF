using System.Windows;
using System.Windows.Controls;
using whr_wpf.Model;
using whr_wpf.ViewModel;

namespace whr_wpf
{
	/// <summary>
	/// TownWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class TownWindow : Window
	{
		private GameInfo gameInfo;

		public TownWindow(TownViewModel vm, GameInfo gameInfo)
		{
			InitializeComponent();

			this.gameInfo = gameInfo;
			this.DataContext = vm;
			Loaded += (s, e) => { AddLineButtons(vm); };
		}
		public void SetDataContext(TownViewModel vm)
		{
			this.DataContext = vm;
		}

		private void AddLineButtons(TownViewModel vm)
		{
			foreach (var line in vm.Lines)
			{
				Button button = new Button();
				button.Content = $"{line.Name} {line.Start.Name}～{line.End.Name}";
				button.HorizontalAlignment = HorizontalAlignment.Center;
				button.VerticalAlignment = VerticalAlignment.Top;
				button.Width = 205;
				button.Height = 30;
				button.FontSize = 16;
				button.Click += (sender, e) =>
				{
					this.Close();

					var page = new LineInfoPage(line, gameInfo);
					var window = new ToolWindow(page);

					window.ShowDialog();
				};

				CommandPanel.Children.Add(button);
			}
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
