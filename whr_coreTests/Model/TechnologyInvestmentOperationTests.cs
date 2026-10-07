using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class TechnologyInvestmentOperationTests
    {
        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        public void SettingChangesOnlyItsDepartmentAndKeepsCurrentInputAcceptance(int department)
        {
            var game = Game();
            game.weeklyInvestment.steam = InvestmentAmountEnum.MN2000;
            game.weeklyInvestment.electricMotor = InvestmentAmountEnum.MN5000;
            game.weeklyInvestment.diesel = InvestmentAmountEnum.OK1;
            game.weeklyInvestment.linearMotor = InvestmentAmountLinearEnum.OK10;
            game.weeklyInvestment.newPlan = InvestmentAmountEnum.OK5;
            var expected = Amounts(game);
            long money = game.Money;
            var accumulated = game.AccumulatedInvest;
            var technologies = (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, DevelopedMask(game));
            int notifications = 0;
            game.PropertyChanged += (_, _) => notifications++;

            // 0・未定義値の受理は新ルールではなく、今回変更しない現状を記録する。
            int valid = department == 3 ? (int)InvestmentAmountLinearEnum.OK25 : (int)InvestmentAmountEnum.OK10;
            foreach (int amount in new[] { valid, 0, -123 })
            {
                Set(game, department, amount);
                expected[department] = amount;
                CollectionAssert.AreEqual(expected, Amounts(game));
            }
            Assert.AreEqual(technologies, (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, DevelopedMask(game)));

            game.genkaiJoki = 150;
            game.genkaiDenki = game.genkaiKidosha = game.genkaiLinear = 990;
            game.isDevelopedBlockingSignal = game.isDevelopedConvertibleCross = game.isDevelopedAutoGate = true;
            game.isDevelopedCarTiltPendulum = game.isDevelopedRichCross = game.isDevelopedRetructableLong = true;
            game.isDevelopedDualSeat = game.isDevelopedMachineTilt = game.isDevelopedFreeGauge = game.isDevelopedDynamicSignal = true;
            Assert.IsFalse(new[] { game.CanSteamDevelop(), game.CanElectricMotorDevelop(), game.CanDieselDevelop(), game.CanLinearMotorDevelop(), game.CanNewPlanDevelop() }[department]);
            technologies = (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, DevelopedMask(game));
            Set(game, department, valid);
            expected[department] = valid;

            CollectionAssert.AreEqual(expected, Amounts(game));
            Assert.AreEqual(technologies, (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, DevelopedMask(game)));
            Assert.AreEqual(money, game.Money);
            Assert.AreEqual(accumulated, game.AccumulatedInvest);
            Assert.AreEqual(0, notifications);
        }

        private static int[] Amounts(GameInfo game) =>
            [(int)game.weeklyInvestment.steam, (int)game.weeklyInvestment.electricMotor, (int)game.weeklyInvestment.diesel,
                (int)game.weeklyInvestment.linearMotor, (int)game.weeklyInvestment.newPlan];

        private static void Set(GameInfo game, int department, int amount)
        {
            switch (department)
            {
                case 0: game.SetSteamInvestment((InvestmentAmountEnum)amount); break;
                case 1: game.SetElectricInvestment((InvestmentAmountEnum)amount); break;
                case 2: game.SetDieselInvestment((InvestmentAmountEnum)amount); break;
                case 3: game.SetLinearInvestment((InvestmentAmountLinearEnum)amount); break;
                case 4: game.SetNewPlanInvestment((InvestmentAmountEnum)amount); break;
                default: throw new ArgumentOutOfRangeException(nameof(department));
            }
        }
    }
}
