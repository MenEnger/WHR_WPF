using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using whr_wpf.Model;
using whr_wpf.View.Technology;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.View.Tests
{
    [TestClass]
    [TestCategory("UiRegression")]
    public class TechnologyInvestmentConnectionTests
    {
        [TestMethod]
        public void ActualWindowSelectionsConnectAllFiveSettersWithoutDuplicateNotifications()
        {
            OnStaThread(() =>
            {
                var game = Game();
                game.genkaiJoki = 100;
                game.genkaiDenki = game.genkaiKidosha = 200;
                game.genkaiLinear = 300;
                var window = new TechnologyDevelopWindow(game);
                try
                {
                    PumpBindings(window);
                    var combos = InvestmentCombos(window);
                    object[] settings = [InvestmentAmountEnum.MN2000, InvestmentAmountEnum.MN5000,
                        InvestmentAmountEnum.OK1, InvestmentAmountLinearEnum.OK10, InvestmentAmountEnum.OK5];
                    int notifications = 0;
                    game.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(GameInfo.weeklyInvestment)) notifications++;
                    };
                    for (int i = 0; i < combos.Length; i++)
                    {
                        combos[i].SelectedValue = settings[i];
                        PumpBindings(window);
                        Assert.AreEqual(settings[i], combos[i].SelectedValue);
                        Assert.AreEqual(i + 1, notifications);
                    }
                    Assert.AreEqual((InvestmentAmountEnum.MN2000, InvestmentAmountEnum.MN5000, InvestmentAmountEnum.OK1,
                            InvestmentAmountLinearEnum.OK10, InvestmentAmountEnum.OK5),
                        (game.weeklyInvestment.steam, game.weeklyInvestment.electricMotor, game.weeklyInvestment.diesel,
                            game.weeklyInvestment.linearMotor, game.weeklyInvestment.newPlan));
                }
                // 実Windowを閉じ、既存のClosedハンドラーでlistenerを解放する。
                finally { window.Close(); }
            });
        }

        [TestMethod]
        public void RejectedSelectionReturnsToRetainedAmountAndShowsUnavailableState()
        {
            OnStaThread(() =>
            {
                var game = Game();
                game.genkaiDenki = 80;
                game.SetDieselInvestment(InvestmentAmountEnum.MN2000);
                var window = new TechnologyDevelopWindow(game);
                try
                {
                    PumpBindings(window);
                    var diesel = InvestmentCombos(window)[2];
                    Assert.AreEqual(Visibility.Visible, diesel.Visibility);
                    // 前提が変わっても古い表示から操作された場合、本体で拒否する。
                    game.genkaiDenki = 79;
                    int notifications = 0;
                    game.PropertyChanged += (_, _) => notifications++;
                    diesel.SelectedValue = InvestmentAmountEnum.OK1;
                    PumpBindings(window);
                    Assert.AreEqual(InvestmentAmountEnum.MN2000, game.weeklyInvestment.diesel);
                    Assert.AreEqual(InvestmentAmountEnum.MN2000, diesel.SelectedValue);
                    Assert.AreEqual(Visibility.Collapsed, diesel.Visibility);
                    var grid = (Grid)diesel.Parent;
                    var label = grid.Children.OfType<Label>().Single(item => Grid.GetRow(item) == 3 && Grid.GetColumn(item) == 1);
                    Assert.AreEqual("(投資不可)", label.Content);
                    Assert.AreEqual(Visibility.Visible, label.Visibility);
                    Assert.AreEqual(0, notifications);
                }
                finally { window.Close(); }
            });
        }

        [TestMethod]
        public void YearEndStopUpdatesTheActualSelectionAndAvailability()
        {
            OnStaThread(() =>
            {
                var game = Game();
                game.SteamYear = 1881;
                game.SetSteamInvestment(InvestmentAmountEnum.MN2000);
                YearEnd(game);
                var window = new TechnologyDevelopWindow(game);
                try
                {
                    PumpBindings(window);
                    var steam = InvestmentCombos(window)[0];
                    Assert.AreEqual(InvestmentAmountEnum.MN2000, steam.SelectedValue);
                    game.NextWeek();
                    PumpBindings(window);
                    Assert.AreEqual(InvestmentAmountEnum.Nothing, steam.SelectedValue);
                    Assert.AreEqual(Visibility.Collapsed, steam.Visibility);
                    Assert.AreEqual(InvestmentAmountEnum.Nothing, game.weeklyInvestment.steam);
                }
                finally { window.Close(); }
            });
        }

        private static ComboBox[] InvestmentCombos(TechnologyDevelopWindow window)
        {
            var panel = (StackPanel)((Grid)window.Content).Children[0];
            return ((Grid)panel.Children[1]).Children.OfType<ComboBox>().OrderBy(Grid.GetRow).ToArray();
        }

        private static void PumpBindings(TechnologyDevelopWindow window)
            => window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

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
