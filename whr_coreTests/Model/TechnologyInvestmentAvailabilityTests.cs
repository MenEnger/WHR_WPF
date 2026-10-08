using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("SelectedBehavior")]
    public class TechnologyInvestmentAvailabilityTests
    {
        [DataTestMethod]
        [DataRow(TechnologyInvestmentDepartment.Steam)]
        [DataRow(TechnologyInvestmentDepartment.Electric)]
        [DataRow(TechnologyInvestmentDepartment.Diesel)]
        [DataRow(TechnologyInvestmentDepartment.Linear)]
        [DataRow(TechnologyInvestmentDepartment.NewPlan)]
        public void UnavailableSettingRejectsWithoutChangesButAllowsZeroAndKeepsFailureSnapshot(TechnologyInvestmentDepartment department)
        {
            var game = UnavailableGame();
            Seed(game, department);
            var before = game.weeklyInvestment;
            var accumulated = game.AccumulatedInvest;
            var technologies = (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, DevelopedMask(game));
            long money = game.Money;
            var observed = new List<InvestmentAmount>();
            game.PropertyChanged += (_, args) =>
            {
                Assert.AreEqual(nameof(GameInfo.weeklyInvestment), args.PropertyName);
                observed.Add(game.weeklyInvestment);
            };

            var rejected = Assert.ThrowsException<TechnologyInvestmentRejectedException>(() => Set(game, department, 200));
            Assert.AreEqual(new TechnologyInvestmentFailure(department, 200), rejected.Failure);
            Assert.AreEqual(before, game.weeklyInvestment);
            Assert.AreEqual(0, observed.Count);
            Set(game, department, 0);
            Assert.AreEqual(0, Amount(game, department));
            Assert.AreEqual(1, observed.Count);
            Assert.AreEqual(game.weeklyInvestment, observed.Single());
            Set(game, department, 0);
            Assert.AreEqual(1, observed.Count);
            Assert.AreEqual(money, game.Money);
            Assert.AreEqual(accumulated, game.AccumulatedInvest);
            Assert.AreEqual(technologies, (game.genkaiJoki, game.genkaiDenki, game.genkaiKidosha, game.genkaiLinear, DevelopedMask(game)));
            Assert.AreEqual(new TechnologyInvestmentFailure(department, 200), rejected.Failure);
        }

        [DataTestMethod]
        [DataRow(TechnologyInvestmentDepartment.Steam)]
        [DataRow(TechnologyInvestmentDepartment.Electric)]
        [DataRow(TechnologyInvestmentDepartment.Diesel)]
        [DataRow(TechnologyInvestmentDepartment.Linear)]
        [DataRow(TechnologyInvestmentDepartment.NewPlan)]
        public void WeeklyBoundaryStopsUnavailableInvestmentBeforePaymentAndDoesNotRestart(TechnologyInvestmentDepartment department)
        {
            var game = UnavailableGame();
            // 前提喪失は初期化や外部のfield変更でも起こる。支払境界で再検査する。
            if (department == TechnologyInvestmentDepartment.Electric) { game.genkaiJoki = 79; }
            if (department == TechnologyInvestmentDepartment.Diesel) { game.genkaiDenki = 79; }
            if (department == TechnologyInvestmentDepartment.Linear) { game.genkaiDenki = game.genkaiKidosha = 199; }
            Seed(game, department);
            game.AccumulatedInvest = default;
            long money = game.Money;
            var observed = new List<(string? Name, int Amount, long Money)>();
            game.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(GameInfo.weeklyInvestment) || args.PropertyName == nameof(GameInfo.AccumulatedInvest))
                    observed.Add((args.PropertyName, Amount(game, department), game.Money));
            };

            game.NextWeek();
            Assert.AreEqual(0, Amount(game, department));
            Assert.AreEqual(money, game.Money);
            Assert.AreEqual(default(InvestmentAmountAccumulated), game.AccumulatedInvest);
            CollectionAssert.AreEqual(new[] { (nameof(GameInfo.weeklyInvestment), 0, money),
                (nameof(GameInfo.AccumulatedInvest), 0, money) }, observed);
            observed.Clear();
            // 可へ戻っても以前の設定額を復活させない。
            game.genkaiJoki = 100;
            game.genkaiDenki = game.genkaiKidosha = 200;
            game.genkaiLinear = 300;
            game.isDevelopedDynamicSignal = false;
            game.NextWeek();
            CollectionAssert.AreEqual(new[] { (nameof(GameInfo.AccumulatedInvest), 0, money) }, observed);
        }

        [DataTestMethod]
        [DataRow(79, 0, 1000000L, 0L)]
        [DataRow(80, 40, 999800L, 200L)]
        public void NormalDieselSettingAndDevelopmentRequireElectricLevel80(int electricLevel, int dieselLevel, long money, long addedInvestment)
        {
            var game = Game();
            game.genkaiDenki = electricLevel;
            game.AccumulatedInvest = new InvestmentAmountAccumulated { diesel = 300001 };
            if (electricLevel < 80)
            {
                Assert.ThrowsException<TechnologyInvestmentRejectedException>(() => game.SetDieselInvestment(InvestmentAmountEnum.MN2000));
                game.weeklyInvestment.diesel = InvestmentAmountEnum.MN2000;
            }
            else { game.SetDieselInvestment(InvestmentAmountEnum.MN2000); }
            var events = game.NextWeek();
            Assert.AreEqual(dieselLevel, game.genkaiKidosha);
            Assert.AreEqual(money, game.Money);
            Assert.AreEqual(300001 + addedInvestment, game.AccumulatedInvest.diesel);
            Assert.AreEqual(electricLevel == 80 ? 1 : 0, events.OfType<EngineDevelopedEvent>().Count());
        }

        [TestMethod]
        public void SameWeekElectricUnlockUsesExistingDieselAccumulationWithoutRestartingPayment()
        {
            var game = Game();
            game.genkaiDenki = 75;
            game.weeklyInvestment.diesel = InvestmentAmountEnum.MN2000;
            game.AccumulatedInvest = new InvestmentAmountAccumulated { electricMotor = 3070626, diesel = 300001 };
            CollectionAssert.AreEqual(new GameEvent[]
            {
                new EngineDevelopedEvent(PowerEnum.Electricity, EngineDevelopmentKind.SpeedImproved, 75, 80),
                new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.Available, 0, 40)
            }, game.NextWeek());
            Assert.AreEqual(InvestmentAmountEnum.Nothing, game.weeklyInvestment.diesel);
            Assert.AreEqual(1000000L, game.Money);
            Assert.AreEqual(300001L, game.AccumulatedInvest.diesel);
        }

        [TestMethod]
        public void AdvancedDieselKeepsItsIndependentAccumulationWhileUnavailableInvestmentStops()
        {
            var game = Game();
            game.genkaiDenki = 79;
            game.genkaiKidosha = 400;
            game.weeklyInvestment.diesel = InvestmentAmountEnum.MN2000;
            game.AccumulatedInvest = new InvestmentAmountAccumulated { diesel = 1000000000 };
            CollectionAssert.AreEqual(new GameEvent[]
            {
                new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.CostReduced, 400, 410)
            }, game.NextWeek());
            Assert.AreEqual(InvestmentAmountEnum.Nothing, game.weeklyInvestment.diesel);
            Assert.AreEqual(1000000L, game.Money);
            Assert.AreEqual(1000000000L, game.AccumulatedInvest.diesel);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SteamExpiryStopsAfterTheLastEligiblePaymentEvenIfAnnualProcessingFails(bool annualFailure)
        {
            var game = Game();
            game.SteamYear = 1881;
            game.weeklyInvestment.steam = InvestmentAmountEnum.MN2000;
            game.AccumulatedInvest = default;
            YearEnd(game);
            if (annualFailure) { game.warModeList = null!; }
            var stopped = new List<(int Year, InvestmentAmountEnum Amount, long Money)>();
            game.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(GameInfo.weeklyInvestment))
                    stopped.Add((game.Year, game.weeklyInvestment.steam, game.Money));
            };
            if (annualFailure) { Assert.ThrowsException<ArgumentNullException>(() => game.NextWeek()); }
            else { game.NextWeek(); }
            Assert.AreEqual((1881, 1, 1), (game.Year, game.Month, game.Week));
            Assert.AreEqual(200L, game.AccumulatedInvest.steam);
            CollectionAssert.AreEqual(new[] { (1881, InvestmentAmountEnum.Nothing, 999800L) }, stopped);
            if (!annualFailure)
            {
                game.NextWeek();
                Assert.AreEqual(999800L, game.Money);
                Assert.AreEqual(200L, game.AccumulatedInvest.steam);
                Assert.AreEqual(1, stopped.Count);
            }
        }

        [TestMethod]
        public void DevelopmentStopsAreNotifiedOnceAfterTheAccumulationNotification()
        {
            var game = Game();
            game.genkaiJoki = 145;
            game.genkaiDenki = 355;
            game.genkaiLinear = 980;
            game.weeklyInvestment = new InvestmentAmount { steam = InvestmentAmountEnum.MN2000,
                electricMotor = InvestmentAmountEnum.MN2000, linearMotor = InvestmentAmountLinearEnum.OK10,
                newPlan = InvestmentAmountEnum.MN2000 };
            game.AccumulatedInvest = new InvestmentAmountAccumulated { steam = 8000000,
                electricMotor = 250000000, linearMotor = 4000000000, newPlan = 79999800 };
            var before = game.weeklyInvestment;
            var observed = new List<(string? Name, InvestmentAmount Amount)>();
            game.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(GameInfo.Money)) observed.Add((args.PropertyName, game.weeklyInvestment));
            };
            game.NextWeek();
            CollectionAssert.AreEqual(new[] { (nameof(GameInfo.AccumulatedInvest), before),
                (nameof(GameInfo.weeklyInvestment), default(InvestmentAmount)) }, observed);
            Assert.AreEqual((150, 370, 990, 10), (game.genkaiJoki, game.genkaiDenki, game.genkaiLinear, DevelopedCount(game)));
        }

        [TestMethod]
        public void StopNotificationFailureRetainsTheStopWithoutPayingOrAdvancing()
        {
            var game = Game();
            game.weeklyInvestment.diesel = InvestmentAmountEnum.MN2000;
            var accumulated = game.AccumulatedInvest;
            var failure = new InvalidOperationException("試験用の通知失敗");
            game.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(GameInfo.weeklyInvestment)) throw failure;
            };
            Assert.AreSame(failure, Assert.ThrowsException<InvalidOperationException>(() => game.NextWeek()));
            Assert.AreEqual(InvestmentAmountEnum.Nothing, game.weeklyInvestment.diesel);
            Assert.AreEqual(1000000L, game.Money);
            Assert.AreEqual(accumulated, game.AccumulatedInvest);
            Assert.AreEqual((1880, 1, 1), (game.Year, game.Month, game.Week));
        }

        private static GameInfo UnavailableGame()
        {
            var game = Game();
            game.genkaiJoki = 150;
            game.genkaiDenki = game.genkaiKidosha = game.genkaiLinear = 990;
            game.isDevelopedBlockingSignal = game.isDevelopedConvertibleCross = game.isDevelopedAutoGate = true;
            game.isDevelopedCarTiltPendulum = game.isDevelopedRichCross = game.isDevelopedRetructableLong = true;
            game.isDevelopedDualSeat = game.isDevelopedMachineTilt = game.isDevelopedFreeGauge = game.isDevelopedDynamicSignal = true;
            return game;
        }

        private static void Seed(GameInfo game, TechnologyInvestmentDepartment department)
        {
            switch (department)
            {
                case TechnologyInvestmentDepartment.Steam: game.weeklyInvestment.steam = InvestmentAmountEnum.MN2000; break;
                case TechnologyInvestmentDepartment.Electric: game.weeklyInvestment.electricMotor = InvestmentAmountEnum.MN2000; break;
                case TechnologyInvestmentDepartment.Diesel: game.weeklyInvestment.diesel = InvestmentAmountEnum.MN2000; break;
                case TechnologyInvestmentDepartment.Linear: game.weeklyInvestment.linearMotor = InvestmentAmountLinearEnum.OK10; break;
                case TechnologyInvestmentDepartment.NewPlan: game.weeklyInvestment.newPlan = InvestmentAmountEnum.MN2000; break;
            }
        }

        private static int Amount(GameInfo game, TechnologyInvestmentDepartment department) => department switch
        {
            TechnologyInvestmentDepartment.Steam => (int)game.weeklyInvestment.steam,
            TechnologyInvestmentDepartment.Electric => (int)game.weeklyInvestment.electricMotor,
            TechnologyInvestmentDepartment.Diesel => (int)game.weeklyInvestment.diesel,
            TechnologyInvestmentDepartment.Linear => (int)game.weeklyInvestment.linearMotor,
            TechnologyInvestmentDepartment.NewPlan => (int)game.weeklyInvestment.newPlan,
            _ => throw new ArgumentOutOfRangeException(nameof(department))
        };

        private static void Set(GameInfo game, TechnologyInvestmentDepartment department, int amount)
        {
            switch (department)
            {
                case TechnologyInvestmentDepartment.Steam: game.SetSteamInvestment((InvestmentAmountEnum)amount); break;
                case TechnologyInvestmentDepartment.Electric: game.SetElectricInvestment((InvestmentAmountEnum)amount); break;
                case TechnologyInvestmentDepartment.Diesel: game.SetDieselInvestment((InvestmentAmountEnum)amount); break;
                case TechnologyInvestmentDepartment.Linear: game.SetLinearInvestment((InvestmentAmountLinearEnum)amount); break;
                case TechnologyInvestmentDepartment.NewPlan: game.SetNewPlanInvestment((InvestmentAmountEnum)amount); break;
                default: throw new ArgumentOutOfRangeException(nameof(department));
            }
        }
    }
}
