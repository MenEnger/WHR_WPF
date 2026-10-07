using System.Runtime.ExceptionServices;
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
        public void ActualWindowViewModelConnectsAllFiveInvestmentSettersAndGetters()
        {
            OnStaThread(() =>
            {
                var game = Game();
                var window = new TechnologyDevelopWindow(game);
                try
                {
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    var vm = window.DataContext;
                    var settings = new (string Property, object Value)[]
                    {
                        ("SteamInvest", InvestmentAmountEnum.MN2000),
                        ("ElectricInvest", InvestmentAmountEnum.MN5000),
                        ("DieselInvest", InvestmentAmountEnum.OK1),
                        ("LinearInvest", InvestmentAmountLinearEnum.OK10),
                        ("NewPlanInvest", InvestmentAmountEnum.OK5),
                    };
                    foreach (var setting in settings)
                    {
                        var property = vm.GetType().GetProperty(setting.Property)!;
                        property.SetValue(vm, setting.Value);
                        Assert.AreEqual(setting.Value, property.GetValue(vm));
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
