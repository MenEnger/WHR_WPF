using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using whr_wpf.ViewModel;
using whr_wpf.ViewModel.Component;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.View.Tests
{
    [TestClass]
    [TestCategory("UiRegression")]
    public class LineCaptionBindingTests
    {
        [TestMethod]
        public void ActualPageDisplaysSectionsAndKeepsLineReferencesWhenNamesMatch()
        {
            OnStaThread(() =>
            {
                var game = Game();
                var first = whr_wpf.Model.Tests.CurrentBehaviorFixture.Line(game);
                first.Name = "同名線";
                first.Start.Name = "東京";
                first.End.Name = "大阪";
                var second = whr_wpf.Model.Tests.CurrentBehaviorFixture.Line(game);
                second.Name = "同名線";
                second.Start = Town("京都", 100);
                second.End = Town("神戸", 100);
                var page = new LineInfoPage(first, game);
                var combo = (ComboBox)page.FindName("LineList");

                CollectionAssert.AreEqual(game.lines.ToArray(), combo.Items.Cast<LineSelectionItem>().Select(item => item.Line).ToArray());
                Assert.AreSame(first, combo.SelectedValue);
                Assert.AreEqual("同名線 東京～大阪", SelectionText(combo));

                // 同名でも区間の異なる実路線を選び、その参照と表示が切り替わることを確認する。
                combo.SelectedItem = combo.Items.Cast<LineSelectionItem>().Single(item => ReferenceEquals(item.Line, second));
                Assert.AreSame(second, combo.SelectedValue);
                CollectionAssert.AreEqual(game.lines.ToArray(), combo.Items.Cast<LineSelectionItem>().Select(item => item.Line).ToArray());
                Assert.AreEqual("同名線 京都～神戸", SelectionText(combo));
                Assert.AreEqual(2, combo.Items.Count);
            });
        }

        [TestMethod]
        public void TextInputSearchDistinguishesSectionsWhenLineNamesMatch()
        {
            OnStaThread(() =>
            {
                var game = Game();
                var first = whr_wpf.Model.Tests.CurrentBehaviorFixture.Line(game);
                first.Name = "同名線";
                first.Start.Name = "東京";
                first.End.Name = "大阪";
                var second = whr_wpf.Model.Tests.CurrentBehaviorFixture.Line(game);
                second.Name = "同名線";
                second.Start = Town("京都", 100);
                second.End = Town("神戸", 100);
                var page = new LineInfoPage(first, game);
                var combo = (ComboBox)page.FindName("LineList");
                Assert.AreEqual("同名線 東京～大阪", SelectionText(combo));
                var composition = new TextComposition(InputManager.Current, combo, "同名線 京");
                // 実ComboBoxへ文字入力を届け、名前が同じ候補を区間で絞り込む。
                combo.RaiseEvent(new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice, composition)
                {
                    RoutedEvent = TextCompositionManager.TextInputEvent,
                });
                combo.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                Assert.AreSame(second, combo.SelectedValue);
                Assert.AreEqual("同名線 京都～神戸", SelectionText(combo));
            });
        }

        [TestMethod]
        public void ClosingLineSelectionNavigatesWithTheOriginalSelectedLine()
        {
            OnStaThread(() =>
            {
                var game = Game();
                var first = whr_wpf.Model.Tests.CurrentBehaviorFixture.Line(game);
                var second = whr_wpf.Model.Tests.CurrentBehaviorFixture.Line(game);
                second.Name = "選択先";
                var page = new LineInfoPage(first, game);
                var frame = new Frame();
                Assert.IsTrue(frame.Navigate(page));
                frame.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.AreSame(page, frame.Content);
                var combo = (ComboBox)page.FindName("LineList");
                combo.SelectedItem = combo.Items.Cast<LineSelectionItem>().Single(item => ReferenceEquals(item.Line, second));
                Assert.AreSame(second, combo.SelectedValue);

                // ポップアップは開かず、実際の閉じる処理からFrameのページ遷移を行う。
                typeof(LineInfoPage).GetMethod("LineList_DropDownClosed", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(page, new object[] { combo, EventArgs.Empty });
                frame.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                var nextPage = frame.Content as LineInfoPage;
                Assert.IsNotNull(nextPage);
                Assert.AreNotSame(page, nextPage);
                Assert.AreSame(second, ((LineInfoViewModel)nextPage.DataContext).line);
                Assert.AreSame(second, game.lastSeenLine);
                Assert.AreSame(second, ((ComboBox)nextPage.FindName("LineList")).SelectedValue);
            });
        }

        private static string SelectionText(ComboBox combo)
        {
            // 実XAMLとWPFの選択表示を配置し、生成された文字を読む。画面は表示しない。
            combo.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            combo.ApplyTemplate();
            combo.Measure(new Size(400, 40));
            combo.Arrange(new Rect(0, 0, 400, 40));
            combo.UpdateLayout();
            var presenter = Descendants(combo).OfType<ContentPresenter>()
                .Single(item => ReferenceEquals(item.Content, combo.SelectionBoxItem));
            return Descendants(presenter).OfType<TextBlock>().Single().Text;
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                yield return child;
                foreach (var descendant in Descendants(child)) yield return descendant;
            }
        }

        private static void OnStaThread(Action action)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
