using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class LineEquipmentRefusalTests
    {
        [DataTestMethod]
        [DataRow("speed", LineEquipmentRejectionReason.SpeedUpUnavailable)]
        [DataRow("unelectrify", LineEquipmentRejectionReason.UnElectrifyUnavailable)]
        [DataRow("electrify", LineEquipmentRejectionReason.ElectrifyUnavailable)]
        [DataRow("narrow", LineEquipmentRejectionReason.NarrowGaugeUnavailable)]
        [DataRow("regular", LineEquipmentRejectionReason.ExpanseGaugeUnavailable)]
        [DataRow("add", LineEquipmentRejectionReason.AddLaneUnavailable)]
        [DataRow("reduce", LineEquipmentRejectionReason.ReduceUnavailable)]
        [DataRow("taihi", LineEquipmentRejectionReason.TaihiUnavailable)]
        public void InitialRefusalPrecedesGameOrCostAccessAndPreservesAllocation(string operation, LineEquipmentRejectionReason reason)
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game);
            line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            through.SettingComposition(stock, 60, game);
            line.IsExist = false;
            // 費用計算へ進むと異常なレーン数・null GameInfo・未定義待避線に触れる。
            // 冒頭拒否ではそれらを調べず、既存の直接運行・系統割当も維持する。
            line.LaneNum = 0;
            var prior = Snapshot(line, through, stock, game);
            var nullGameRejection = Assert.ThrowsException<LineEquipmentRejectedException>(() => Execute(operation, line, null!));
            Assert.AreEqual(ExpectedFailure(line, reason), nullGameRejection.Failure);
            var rejection = Assert.ThrowsException<LineEquipmentRejectedException>(() => Execute(operation, line, game));
            Assert.AreEqual(ExpectedFailure(line, reason), rejection.Failure);
            Assert.AreEqual(prior, Snapshot(line, through, stock, game));
        }

        [TestMethod]
        public void SpeedUpUsesCurrentSpeedAndExactFifthUpgradeLimit()
        {
            var game = Game();
            var line = Line(game);
            line.bestSpeedUpKaisu = 4;
            Assert.IsTrue(line.CanSpeedUp());
            line.bestSpeedUpKaisu = 5;
            Assert.IsFalse(line.CanSpeedUp());
            line.bestSpeedUpKaisu = 6;
            // 現状は5との一致のみを検査する。6以上を拒否する仕様修正は混ぜない。
            Assert.IsTrue(line.CanSpeedUp());
            line.bestSpeedUpKaisu = 0;
            line.bestSpeed = 299;
            Assert.IsTrue(line.CanSpeedUp());
            line.bestSpeed = 300;
            Assert.IsFalse(line.CanSpeedUp());
            line.gauge = RailGaugeEnum.Regular;
            line.bestSpeed = 359;
            Assert.IsTrue(line.CanSpeedUp());
            line.bestSpeed = 360;
            Assert.IsFalse(line.CanSpeedUp());
            line.Type = RailTypeEnum.LinearMotor;
            line.bestSpeed = 989;
            Assert.IsTrue(line.CanSpeedUp());
            line.bestSpeed = 990;
            Assert.IsFalse(line.CanSpeedUp());
        }

        [TestMethod]
        public void TaihiChangeStopsAtFourLanesAndRefusesBeforeReadingNewInterval()
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = 3;
            Assert.IsTrue(line.CanTaihiChange());
            line.LaneNum = 4;
            Assert.IsFalse(line.CanTaihiChange());
            var rejection = Assert.ThrowsException<LineEquipmentRejectedException>(() => line.ChangeTaihi((TaihisenEnum)999, null!));
            Assert.AreEqual(ExpectedFailure(line, LineEquipmentRejectionReason.TaihiUnavailable), rejection.Failure);
            Assert.AreEqual((4, TaihisenEnum.Every20km), (line.LaneNum, line.taihisen));
        }

        [TestMethod]
        public void ElectrificationNullableAndTrackTypeControlBothOperations()
        {
            var game = Game();
            var line = Line(game);
            Assert.AreEqual((true, false), (line.CanElectrify(), line.CanUnElectrify()));
            line.IsElectrified = true;
            Assert.AreEqual((false, true), (line.CanElectrify(), line.CanUnElectrify()));
            line.IsElectrified = null;
            Assert.AreEqual((false, false), (line.CanElectrify(), line.CanUnElectrify()));
            Assert.AreEqual(ExpectedFailure(line, LineEquipmentRejectionReason.ElectrifyUnavailable),
                Assert.ThrowsException<LineEquipmentRejectedException>(() => line.Electrify(null!)).Failure);
            Assert.AreEqual(ExpectedFailure(line, LineEquipmentRejectionReason.UnElectrifyUnavailable),
                Assert.ThrowsException<LineEquipmentRejectedException>(() => line.UnElectrify(null!)).Failure);
            line.Type = RailTypeEnum.LinearMotor;
            line.IsElectrified = false;
            Assert.IsFalse(line.CanElectrify());
            line.IsElectrified = true;
            Assert.IsFalse(line.CanUnElectrify());
        }

        [TestMethod]
        public void GaugeChangeRequiresOppositeOrdinaryGaugeOnIronTrack()
        {
            var game = Game();
            var line = Line(game);
            Assert.AreEqual((false, true), (line.CanNarrowGauge(), line.CanExpanseGauge()));
            line.gauge = RailGaugeEnum.Regular;
            Assert.AreEqual((true, false), (line.CanNarrowGauge(), line.CanExpanseGauge()));
            line.Type = RailTypeEnum.LinearMotor;
            Assert.IsFalse(line.CanNarrowGauge());
            line.gauge = RailGaugeEnum.Narrow;
            Assert.IsFalse(line.CanExpanseGauge());
            line.Type = RailTypeEnum.Iron;
            line.gauge = (RailGaugeEnum)999;
            Assert.AreEqual((false, false), (line.CanNarrowGauge(), line.CanExpanseGauge()));
        }

        [TestMethod]
        public void AllowedOperationKeepsInternalLaneAndMoneyExceptionsDistinctFromInitialRefusal()
        {
            var game = Game();
            var line = Line(game);
            line.LaneNum = 0;
            Assert.IsTrue(line.CanAddLane());
            var internalError = Assert.ThrowsException<InvalidOperationException>(() => line.AddLane(game));
            Assert.AreEqual("レーン数が異常です", internalError.Message);
            Assert.AreEqual((0, 1000000L, 0L), (line.LaneNum, game.Money, line.totalBalance));
            line.LaneNum = 2;
            game.Money = 0;
            Assert.IsTrue(line.CanElectrify());
            Assert.ThrowsException<MoneyShortException>(() => line.Electrify(game));
            Assert.AreEqual(((bool?)false, 0L, 0L), (line.IsElectrified, game.Money, line.totalBalance));
        }

        [TestMethod]
        public void EquipmentFailureKeepsRejectedStateAfterLaterLineChanges()
        {
            var game = Game();
            var line = Line(game);
            line.bestSpeedUpKaisu = 5;
            var expected = ExpectedFailure(line, LineEquipmentRejectionReason.SpeedUpUnavailable);
            var failure = Assert.ThrowsException<LineEquipmentRejectedException>(() => line.SpeedUp(game)).Failure;
            line.Name = "変更後";
            line.IsExist = false;
            line.bestSpeed = 360;
            line.bestSpeedUpKaisu = 0;
            line.Type = RailTypeEnum.LinearMotor;
            line.gauge = RailGaugeEnum.Regular;
            Assert.AreEqual(expected, failure);
        }

        private static LineEquipmentFailure ExpectedFailure(Line line, LineEquipmentRejectionReason reason)
        {
            var expected = new LineEquipmentFailure(reason, line.Name, line.IsExist);
            // 全体比較で操作に必要な状態と無関係な値のnullを同時に確認する。
            return reason switch
            {
                LineEquipmentRejectionReason.SpeedUpUnavailable => expected with
                {
                    BestSpeed = line.bestSpeed, ImprovementCount = line.bestSpeedUpKaisu,
                    Type = line.Type, Gauge = line.gauge
                },
                LineEquipmentRejectionReason.UnElectrifyUnavailable or LineEquipmentRejectionReason.ElectrifyUnavailable =>
                    expected with { Type = line.Type, IsElectrified = line.IsElectrified },
                LineEquipmentRejectionReason.NarrowGaugeUnavailable or LineEquipmentRejectionReason.ExpanseGaugeUnavailable =>
                    expected with { Type = line.Type, Gauge = line.gauge },
                LineEquipmentRejectionReason.TaihiUnavailable => expected with { LaneCount = line.LaneNum },
                LineEquipmentRejectionReason.AddLaneUnavailable or LineEquipmentRejectionReason.ReduceUnavailable => expected,
                _ => throw new AssertFailedException("未検証の拒否理由です")
            };
        }

        private static void Execute(string operation, Line line, GameInfo game)
        {
            switch (operation)
            {
                case "speed": line.SpeedUp(game); break;
                case "unelectrify": line.UnElectrify(game); break;
                case "electrify": line.Electrify(game); break;
                case "narrow": line.NarrowGauge(game); break;
                case "regular": line.ExpanseGauge(game); break;
                case "add": line.AddLane(game); break;
                case "reduce": line.ReduceOrRemoveLane(game); break;
                case "taihi": line.ChangeTaihi((TaihisenEnum)999, game); break;
                default: throw new AssertFailedException("未検証の設備操作です");
            }
        }

        private static object Snapshot(Line line, KeitoDiagram through, IComposition stock, GameInfo game) => new
        {
            game.Money, game.outlay, line.totalBalance, line.IsExist, line.IsSuspended,
            line.Type, line.IsElectrified, line.gauge, line.bestSpeed, line.bestSpeedUpKaisu,
            line.LaneNum, line.taihisen, line.diagram, line.useComposition, line.useCompositionNum,
            line.runningPerDay, stock.HeldUnits,
            ThroughComposition = through.useComposition, ThroughUnits = through.useCompositionNum,
            ThroughRunningPerDay = through.runningPerDay
        };
    }
}
