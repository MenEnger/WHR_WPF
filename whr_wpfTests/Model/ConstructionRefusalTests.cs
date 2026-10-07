using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class ConstructionRefusalTests
    {
        [DataTestMethod]
        [DataRow(20, RailTypeEnum.Iron, false, 2)]
        [DataRow(60, RailTypeEnum.Iron, false, 0)]
        [DataRow(60, (RailTypeEnum)999, false, 2)]
        [DataRow(60, RailTypeEnum.Iron, null, 2)]
        public void InvalidConstructionSettingsRejectWithoutChangingStateOrNotifying(int speed, RailTypeEnum type, bool? electrified, int lanes)
        {
            var game = Game();
            var line = Line(game, false);
            line.bestSpeedUpKaisu = 3;
            line.totalBalance = -123;
            var previous = Snapshot(line, game);
            var notifications = new List<string>();
            game.PropertyChanged += (_, args) => notifications.Add("game:" + args.PropertyName);
            line.PropertyChanged += (_, args) => notifications.Add("line:" + args.PropertyName);
            Assert.IsFalse(line.CanConstruct(speed, type, electrified, RailGaugeEnum.Narrow, lanes));
            var exception = Assert.ThrowsException<LineConstructionRejectedException>(() =>
                line.Construct(speed, type, electrified, RailGaugeEnum.Narrow, lanes, TaihisenEnum.Every50km, game));
            Assert.AreEqual(new LineConstructionFailure(line.Name, speed, type, electrified, RailGaugeEnum.Narrow, lanes), exception.Failure);
            Assert.IsInstanceOfType<InvalidOperationException>(exception);
            Assert.AreEqual(previous, Snapshot(line, game));
            Assert.AreEqual(0, notifications.Count);
        }

        [TestMethod]
        public void InvalidSettingsPrecedeNullGameAndInvalidTaihiCostArguments()
        {
            var game = Game();
            var line = Line(game, false);
            var exception = Assert.ThrowsException<LineConstructionRejectedException>(() =>
                line.Construct(20, RailTypeEnum.Iron, null, null, -1, (TaihisenEnum)999, null!));
            Assert.AreEqual(new LineConstructionFailure(line.Name, 20, RailTypeEnum.Iron, null, null, -1), exception.Failure);
            Assert.AreEqual((false, 60, 2, 1000000L), (line.IsExist, line.bestSpeed, line.LaneNum, game.Money));
        }

        [TestMethod]
        public void RejectedConstructionSettingsRemainSnapshotsAfterLineChanges()
        {
            var game = Game();
            var line = Line(game, false);
            var failure = Assert.ThrowsException<LineConstructionRejectedException>(() =>
                line.Construct(20, (RailTypeEnum)999, null, null, -1, TaihisenEnum.None, game)).Failure;
            line.Name = "変更後";
            line.bestSpeed = 100;
            line.Type = RailTypeEnum.LinearMotor;
            line.IsElectrified = true;
            line.gauge = RailGaugeEnum.Regular;
            line.LaneNum = 4;
            Assert.AreEqual(new LineConstructionFailure("試験線", 20, (RailTypeEnum)999, null, null, -1), failure);
        }

        private static object Snapshot(Line line, GameInfo game) => new
        {
            game.Money, game.income, game.outlay, line.IsExist, line.IsSuspended, line.bestSpeed,
            line.bestSpeedUpKaisu, line.Type, line.IsElectrified, line.gauge, line.LaneNum,
            line.taihisen, line.totalBalance, line.diagram, line.useComposition, line.useCompositionNum, line.runningPerDay
        };
    }
}
