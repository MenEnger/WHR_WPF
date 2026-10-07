using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class ServiceSettingRefusalTests
    {
        [DataTestMethod]
        [DataRow(false, ServiceSettingRejectionReason.Unavailable)]
        [DataRow(true, ServiceSettingRejectionReason.Unavailable)]
        [DataRow(true, ServiceSettingRejectionReason.FrequencyExceeded)]
        [DataRow(false, ServiceSettingRejectionReason.StockUnavailable)]
        [DataRow(true, ServiceSettingRejectionReason.StockUnavailable)]
        public void InitialRefusalCapturesComparedValuesAndPreservesExistingAllocation(bool throughService, ServiceSettingRejectionReason reason)
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var oldStock = Stock(game);
            Set(throughService, line, through, oldStock, 60, game);
            var replacement = Stock(game, 0);
            if (reason == ServiceSettingRejectionReason.Unavailable) line.IsExist = false;
            // 未建設・頻度拒否が適合検査より先であることも同時に確認する。
            if (reason != ServiceSettingRejectionReason.StockUnavailable) replacement.Type = RailTypeEnum.LinearMotor;
            var previous = Snapshot(line, through, oldStock, replacement, game);
            int requested = reason == ServiceSettingRejectionReason.FrequencyExceeded ? 1000 : 60;
            var exception = Assert.ThrowsException<ServiceSettingRejectedException>(() =>
                Set(throughService, line, through, replacement, requested, game));
            var expected = new ServiceSettingFailure(throughService ? ServiceSettingTarget.Through : ServiceSettingTarget.Direct,
                reason, throughService ? through.Name : line.Name, requested);
            if (reason == ServiceSettingRejectionReason.StockUnavailable)
                expected = expected with { RequiredUnits = 2, AvailableUnits = 0, CalculatedMissingUnits = 2 };
            // 全体比較で、拒否段階では取得しない数量がnullであることも確認する。
            Assert.AreEqual(expected, exception.Failure);
            Assert.AreEqual(previous, Snapshot(line, through, oldStock, replacement, game));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NegativeThenNullGameThenNullCompositionPrecedeInitialServiceRefusals(bool throughService)
        {
            var game = Game();
            var line = Line(game, false);
            var through = Through(game, line);
            var negative = Assert.ThrowsException<ArgumentOutOfRangeException>(() => Set(throughService, line, through, null!, -1, null!));
            Assert.AreEqual("runningPerDay", negative.ParamName);
            var nullGame = Assert.ThrowsException<ArgumentNullException>(() => Set(throughService, line, through, null!, 0, null!));
            Assert.AreEqual("gameInfo", nullGame.ParamName);
            var nullStock = Assert.ThrowsException<ArgumentNullException>(() => Set(throughService, line, through, null!, 0, game));
            Assert.AreEqual("newComposition", nullStock.ParamName);
            Assert.AreEqual((1000000L, 0, 0), (game.Money, line.useCompositionNum, through.useCompositionNum));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompatibleFrequencyChecksVehicleCompatibilityBeforeAvailableStock(bool throughService)
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game, 0);
            stock.Gauge = CarGaugeEnum.Regular;
            var exception = Assert.ThrowsException<CompositionNotAppliedException>(() => Set(throughService, line, through, stock, 60, game));
            Assert.AreEqual(CompositionCompatibilityReason.GaugeMismatch, exception.Failure.Reason);
            Assert.AreEqual((0, 0, 0), (stock.HeldUnits, line.useCompositionNum, through.useCompositionNum));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SameCompositionCanReuseAllocatedUnitsWithNoAvailableStock(bool throughService)
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game, 2);
            Set(throughService, line, through, stock, 60, game);
            Assert.AreEqual(0, stock.HeldUnits);
            Set(throughService, line, through, stock, 60, game);
            Assert.AreEqual(0, stock.HeldUnits);
            Assert.AreEqual(2, throughService ? through.useCompositionNum : line.useCompositionNum);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PreStockCheckReadsLeftAvailabilityThenRecalculatesMissingWithRightAvailability(bool throughService)
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var observed = new ObservedComposition();
            var exception = Assert.ThrowsException<ServiceSettingRejectedException>(() => Set(throughService, line, through, observed, 60, game));
            Assert.AreEqual(new ServiceSettingFailure(throughService ? ServiceSettingTarget.Through : ServiceSettingTarget.Direct,
                ServiceSettingRejectionReason.StockUnavailable, throughService ? through.Name : line.Name, 60)
                { RequiredUnits = 2, AvailableUnits = 0, CalculatedMissingUnits = 2 }, exception.Failure);
            var availabilityPositions = observed.Reads.Select((value, index) => (value, index))
                .Where(item => item.value == "held").Select(item => item.index).ToArray();
            Assert.AreEqual(2, availabilityPositions.Length);
            // 左辺を読む前の必要数計算に加え、左右の在庫参照の間にも必要数を再計算する現状。
            Assert.IsTrue(observed.Reads.Take(availabilityPositions[0]).Contains("speed"));
            Assert.IsTrue(observed.Reads.Skip(availabilityPositions[0] + 1)
                .Take(availabilityPositions[1] - availabilityPositions[0] - 1).Contains("speed"));
            Assert.AreEqual((0, 0), (observed.NameReads, observed.UseCalls));
        }

        [TestMethod]
        public void FailureSnapshotDoesNotFollowLaterTargetOrAvailableStockChanges()
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game, 0);
            var failure = Assert.ThrowsException<ServiceSettingRejectedException>(() => Set(false, line, through, stock, 60, game)).Failure;
            line.Name = "変更後";
            line.IsExist = false;
            stock.Release(10);
            Assert.AreEqual(new ServiceSettingFailure(ServiceSettingTarget.Direct,
                ServiceSettingRejectionReason.StockUnavailable, "試験線", 60)
                { RequiredUnits = 2, AvailableUnits = 0, CalculatedMissingUnits = 2 }, failure);
        }

        [TestMethod]
        public void StockFailureCapturesDifferentLeftAndRightAvailabilityAtTheirEvaluationTimes()
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var observed = new ObservedComposition(rightAvailability: 1);
            var failure = Assert.ThrowsException<ServiceSettingRejectedException>(() => Set(false, line, through, observed, 60, game)).Failure;
            // 比較左辺は0、右辺の内部は1。数量を同時点の値とみなして再計算しない。
            Assert.AreEqual(new ServiceSettingFailure(ServiceSettingTarget.Direct,
                ServiceSettingRejectionReason.StockUnavailable, line.Name, 60)
                { RequiredUnits = 2, AvailableUnits = 0, CalculatedMissingUnits = 1 }, failure);
            Assert.AreEqual(2, observed.HeldReads);
            Assert.AreEqual((0, 0), (observed.NameReads, observed.UseCalls));
        }

        // 必要数計算と在庫参照の境界だけを観測し、その他は既存の既定編成を再利用する。
        private sealed class ObservedComposition : DefautltComposition, IComposition
        {
            private readonly int rightAvailability;
            internal List<string> Reads { get; } = new();
            internal int NameReads, UseCalls, HeldReads;
            internal ObservedComposition(int rightAvailability = 0)
            {
                this.rightAvailability = rightAvailability;
                BestSpeed = 60; CarCount = 8; Power = PowerEnum.Steam;
                Type = RailTypeEnum.Iron; Gauge = CarGaugeEnum.Narrow; seat = SeatEnum.Semi;
            }
            int IComposition.HeldUnits
            {
                get
                {
                    Reads.Add("held");
                    HeldReads++;
                    if (HeldReads > 2) throw new FormatException("余剰数の追加取得");
                    return HeldReads == 1 ? 0 : rightAvailability;
                }
            }
            int IComposition.BestSpeed { get { Reads.Add("speed"); return 60; } set => throw new InvalidOperationException(); }
            string IComposition.Name
            {
                get { NameReads++; throw new FormatException("不要な名前取得"); }
                set => throw new InvalidOperationException();
            }
            void IComposition.Use(int quantity) { UseCalls++; throw new FormatException("事前拒否後の供出"); }
        }

        private static void Set(bool throughService, Line line, KeitoDiagram through, IComposition stock, int count, GameInfo game)
        {
            if (throughService) through.SettingComposition(stock, count, game);
            else line.SettingComposition(stock, count, DiagramType.LimittedExpressPrior, game);
        }

        private static object Snapshot(Line line, KeitoDiagram through, IComposition oldStock, IComposition replacement, GameInfo game) => new
        {
            game.Money, line.IsExist, line.diagram, line.useComposition, line.useCompositionNum, line.runningPerDay,
            ThroughComposition = through.useComposition, ThroughUnits = through.useCompositionNum,
            ThroughRunning = through.runningPerDay, OldAvailable = oldStock.HeldUnits, NewAvailable = replacement.HeldUnits
        };
    }
}
