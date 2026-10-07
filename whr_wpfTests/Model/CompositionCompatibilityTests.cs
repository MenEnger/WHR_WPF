using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("CurrentBehavior")]
    public class CompositionCompatibilityTests
    {
        [DataTestMethod]
        [DataRow("linearLine", CompositionCompatibilityReason.RequiresLinearVehicle)]
        [DataRow("linearStock", CompositionCompatibilityReason.RequiresLinearTrack)]
        [DataRow("narrowStock", CompositionCompatibilityReason.GaugeMismatch)]
        [DataRow("regularStock", CompositionCompatibilityReason.GaugeMismatch)]
        [DataRow("electricStock", CompositionCompatibilityReason.RequiresElectrification)]
        [DataRow("expiredSteam", CompositionCompatibilityReason.SteamExpired)]
        public void CompatibilityBranchesRejectWithCapturedValues(string scenario, CompositionCompatibilityReason reason)
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            switch (scenario)
            {
                case "linearLine": line.Type = RailTypeEnum.LinearMotor; break;
                case "linearStock": stock.Type = RailTypeEnum.LinearMotor; break;
                case "narrowStock": line.gauge = RailGaugeEnum.Regular; break;
                case "regularStock": stock.Gauge = CarGaugeEnum.Regular; break;
                case "electricStock": stock.Power = PowerEnum.Electricity; break;
                case "expiredSteam": game.SteamYear = game.Year; break;
            }
            AssertRejected(line, stock, game, reason);
        }

        [DataTestMethod]
        [DataRow(RailGaugeEnum.Narrow)]
        [DataRow(RailGaugeEnum.Regular)]
        public void FreeGaugeAndMatchingOrdinaryGaugeAreAccepted(RailGaugeEnum gauge)
        {
            var game = Game();
            var line = Line(game);
            line.gauge = gauge;
            var stock = Stock(game);
            stock.Gauge = gauge == RailGaugeEnum.Narrow ? CarGaugeEnum.Narrow : CarGaugeEnum.Regular;
            line.ValidateCompositionAcceptable(stock, game);
            stock.Gauge = CarGaugeEnum.FreeGauge;
            line.ValidateCompositionAcceptable(stock, game);
        }

        [TestMethod]
        public void SteamExpiryBoundaryAndElectricSupplyControlAcceptance()
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            game.SteamYear = game.Year + 1;
            line.ValidateCompositionAcceptable(stock, game);
            game.SteamYear = game.Year;
            AssertRejected(line, stock, game, CompositionCompatibilityReason.SteamExpired);
            stock.Power = PowerEnum.Electricity;
            line.IsElectrified = true;
            line.ValidateCompositionAcceptable(stock, game);
            line.IsElectrified = false;
            AssertRejected(line, stock, game, CompositionCompatibilityReason.RequiresElectrification);
        }

        [TestMethod]
        public void LinearTrackOnlyChecksTrackTypeAndDieselDoesNotReadNullableElectrification()
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            line.Type = stock.Type = RailTypeEnum.LinearMotor;
            line.IsElectrified = null;
            line.gauge = RailGaugeEnum.Regular;
            game.SteamYear = game.Year;
            // リニア軌道では軌間・電化・蒸気年代の後続検査に進まない現状。
            line.ValidateCompositionAcceptable(stock, game);
            line.Type = stock.Type = RailTypeEnum.Iron;
            stock.Gauge = CarGaugeEnum.Regular;
            stock.Power = PowerEnum.Diesel;
            line.ValidateCompositionAcceptable(stock, game);
            stock.Power = PowerEnum.Electricity;
            Assert.ThrowsException<InvalidOperationException>(() => line.ValidateCompositionAcceptable(stock, game));
        }

        [TestMethod]
        public void MultipleViolationsFollowTrackThenGaugeThenElectrificationOrder()
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            stock.Type = RailTypeEnum.LinearMotor;
            stock.Gauge = CarGaugeEnum.Regular;
            stock.Power = PowerEnum.Electricity;
            line.IsElectrified = null;
            AssertRejected(line, stock, game, CompositionCompatibilityReason.RequiresLinearTrack);
            stock.Type = RailTypeEnum.Iron;
            AssertRejected(line, stock, game, CompositionCompatibilityReason.GaugeMismatch);
            stock.Gauge = CarGaugeEnum.Narrow;
            // nullableの内部例外も後続段階に到達した場合に限る。
            Assert.ThrowsException<InvalidOperationException>(() => line.ValidateCompositionAcceptable(stock, game));
        }

        [TestMethod]
        public void NullArgumentsAndUnknownTrackKeepInternalExceptionTypes()
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            var through = Through(game, line);
            var nullStock = Assert.ThrowsException<ArgumentNullException>(() => line.ValidateCompositionAcceptable(null!, game));
            Assert.AreEqual("composition", nullStock.ParamName);
            Assert.ThrowsException<ArgumentNullException>(() => through.ValidateCompositionAcceptable(null!, null!));
            var nullGame = Assert.ThrowsException<ArgumentNullException>(() => through.ValidateCompositionAcceptable(stock, null!));
            Assert.AreEqual("gameInfo", nullGame.ParamName);
            // 路線の直接呼出しは蒸気判定までGameInfoを参照しない。
            stock.Power = PowerEnum.Diesel;
            line.ValidateCompositionAcceptable(stock, null!);
            stock.Power = PowerEnum.Steam;
            Assert.ThrowsException<NullReferenceException>(() => line.ValidateCompositionAcceptable(stock, null!));
            line.Type = (RailTypeEnum)999;
            var unknown = Assert.ThrowsException<InvalidOperationException>(() => line.ValidateCompositionAcceptable(stock, game));
            Assert.AreEqual("未定義の軌道タイプです。検査を追加してください", unknown.Message);
        }

        [TestMethod]
        public void ThroughServiceReportsFirstIncompatibleRouteSection()
        {
            var game = Game();
            var first = Line(game);
            var second = Line(game);
            first.Name = "先行線";
            second.Name = "後続線";
            var stock = Stock(game);
            first.Type = RailTypeEnum.LinearMotor;
            second.gauge = RailGaugeEnum.Regular;
            var through = Through(game, first, second);
            var rejection = Assert.ThrowsException<CompositionNotAppliedException>(() => through.ValidateCompositionAcceptable(stock, game));
            Assert.AreEqual(CompositionCompatibilityReason.RequiresLinearVehicle, rejection.Failure.Reason);
            Assert.AreEqual("先行線", rejection.Failure.LineName);
            through.route.Reverse();
            rejection = Assert.ThrowsException<CompositionNotAppliedException>(() => through.ValidateCompositionAcceptable(stock, game));
            Assert.AreEqual(CompositionCompatibilityReason.GaugeMismatch, rejection.Failure.Reason);
            Assert.AreEqual("後続線", rejection.Failure.LineName);
            second.gauge = RailGaugeEnum.Narrow;
            first.Type = RailTypeEnum.Iron;
            through.ValidateCompositionAcceptable(stock, game);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompatibilityRejectionPreservesExistingAllocationAndMoney(bool throughService)
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            var replacement = Stock(game);
            var through = Through(game, line);
            if (throughService) through.SettingComposition(stock, 60, game);
            else line.SettingComposition(stock, 60, DiagramType.LimittedExpressPrior, game);
            var previous = (game.Money, stock.HeldUnits, replacement.HeldUnits, line.diagram,
                line.useCompositionNum, line.runningPerDay, through.useCompositionNum, through.runningPerDay);
            replacement.Gauge = CarGaugeEnum.Regular;
            if (throughService)
                Assert.ThrowsException<CompositionNotAppliedException>(() => through.SettingComposition(replacement, 110, game));
            else
                Assert.ThrowsException<CompositionNotAppliedException>(() => line.SettingComposition(replacement, 110, DiagramType.Regular, game));
            Assert.AreEqual(previous, (game.Money, stock.HeldUnits, replacement.HeldUnits, line.diagram,
                line.useCompositionNum, line.runningPerDay, through.useCompositionNum, through.runningPerDay));
            Assert.AreSame(stock, throughService ? through.useComposition : line.useComposition);
        }

        [TestMethod]
        public void FailureSnapshotDoesNotFollowLaterLineStockOrGameChanges()
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            game.SteamYear = game.Year;
            var failure = Assert.ThrowsException<CompositionNotAppliedException>(() =>
                line.ValidateCompositionAcceptable(stock, game)).Failure;
            line.Name = "変更後";
            line.Type = RailTypeEnum.LinearMotor;
            stock.Power = PowerEnum.Diesel;
            game.SteamYear = game.Year + 1;
            Assert.AreEqual(new CompositionCompatibilityFailure(CompositionCompatibilityReason.SteamExpired, "試験線")
                { Year = 1880, SteamEndYear = 1880 }, failure);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AllocationRechecksLineChangedAfterSuccessfulPrecheck(bool throughService)
        {
            var game = Game();
            var line = Line(game);
            var stock = Stock(game);
            var through = Through(game, line);
            if (throughService) through.ValidateCompositionAcceptable(stock, game);
            else line.ValidateCompositionAcceptable(stock, game);
            line.gauge = RailGaugeEnum.Regular;
            var previous = (game.Money, stock.HeldUnits, line.diagram);
            var exception = Assert.ThrowsException<CompositionNotAppliedException>(() =>
            {
                if (throughService) through.SettingComposition(stock, 60, game);
                else line.SettingComposition(stock, 60, DiagramType.Regular, game);
            });
            Assert.AreEqual(new CompositionCompatibilityFailure(CompositionCompatibilityReason.GaugeMismatch, line.Name)
                { LineGauge = RailGaugeEnum.Regular, CompositionGauge = CarGaugeEnum.Narrow }, exception.Failure);
            Assert.AreEqual(previous, (game.Money, stock.HeldUnits, line.diagram));
            Assert.IsNull(line.useComposition);
            Assert.IsNull(through.useComposition);
        }

        [TestMethod]
        public void TrackMismatchReadsTypeOnceAndDoesNotReadFollowingCompositionProperties()
        {
            var game = Game();
            var line = Line(game);
            var stock = new ObservedComposition { Type = RailTypeEnum.LinearMotor };
            var exception = Assert.ThrowsException<CompositionNotAppliedException>(() => line.ValidateCompositionAcceptable(stock, game));
            Assert.AreEqual(new CompositionCompatibilityFailure(CompositionCompatibilityReason.RequiresLinearTrack, line.Name)
                { LineType = RailTypeEnum.Iron, CompositionType = RailTypeEnum.LinearMotor }, exception.Failure);
            Assert.AreEqual((1, 0, 0, 0, 0),
                (stock.TypeReads, stock.GaugeReads, stock.ElectrificationReads, stock.PowerReads, stock.NameReads));
        }

        [TestMethod]
        public void GaugeMismatchKeepsExistingGaugeReadCountWithoutReadingLaterProperties()
        {
            var game = Game();
            var line = Line(game);
            var stock = new ObservedComposition { Type = RailTypeEnum.Iron, Gauge = CarGaugeEnum.Regular };
            var exception = Assert.ThrowsException<CompositionNotAppliedException>(() => line.ValidateCompositionAcceptable(stock, game));
            Assert.AreEqual(new CompositionCompatibilityFailure(CompositionCompatibilityReason.GaugeMismatch, line.Name)
                { LineGauge = RailGaugeEnum.Narrow, CompositionGauge = CarGaugeEnum.Regular }, exception.Failure);
            // 標準軌の編成は狭軌比較と標準軌比較で2回読む現状を維持する。
            Assert.AreEqual((1, 2, 0, 0, 0),
                (stock.TypeReads, stock.GaugeReads, stock.ElectrificationReads, stock.PowerReads, stock.NameReads));
        }

        // 比較順の代表ケースだけを観測する。適合検査が不要な情報を読むと例外になる。
        private sealed class ObservedComposition : IComposition
        {
            private RailTypeEnum type;
            private CarGaugeEnum? gauge;
            internal int TypeReads, GaugeReads, ElectrificationReads, PowerReads, NameReads;
            public RailTypeEnum Type { get { TypeReads++; return type; } set => type = value; }
            public CarGaugeEnum? Gauge { get { GaugeReads++; return gauge; } set => gauge = value; }
            public bool IsElectrified { get { ElectrificationReads++; throw new InvalidOperationException(); } }
            public PowerEnum Power { get { PowerReads++; throw new InvalidOperationException(); } set => throw new InvalidOperationException(); }
            public string Name { get { NameReads++; throw new InvalidOperationException(); } set => throw new InvalidOperationException(); }
            public int BestSpeed { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
            public CarTiltEnum Tilt { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
            public int CarCount => throw new InvalidOperationException();
            public int PassengerCapacity => throw new InvalidOperationException();
            public int Price => throw new InvalidOperationException();
            public int HeldUnits => throw new InvalidOperationException();
            public SeatEnum? BestComfortSeat => throw new InvalidOperationException();
            public void Purchase(GameInfo gameInfo, int quantity) => throw new InvalidOperationException();
            public void Use(int quantity) => throw new InvalidOperationException();
            public void Release(int quantity) => throw new InvalidOperationException();
        }

        private static void AssertRejected(Line line, IComposition stock, GameInfo game, CompositionCompatibilityReason reason)
        {
            var exception = Assert.ThrowsException<CompositionNotAppliedException>(() => line.ValidateCompositionAcceptable(stock, game));
            var expected = new CompositionCompatibilityFailure(reason, line.Name);
            expected = reason switch
            {
                CompositionCompatibilityReason.RequiresLinearVehicle or CompositionCompatibilityReason.RequiresLinearTrack =>
                    expected with { LineType = line.Type, CompositionType = stock.Type },
                CompositionCompatibilityReason.GaugeMismatch =>
                    expected with { LineGauge = line.gauge, CompositionGauge = stock.Gauge },
                CompositionCompatibilityReason.RequiresElectrification =>
                    expected with { LineElectrified = false, CompositionElectrified = true },
                CompositionCompatibilityReason.SteamExpired =>
                    expected with { Year = game.Year, SteamEndYear = game.SteamYear },
                _ => throw new AssertFailedException("未検証の拒否理由です"),
            };
            // レコード全体の比較により、対象値と無関係な値のnullを同時に確認する。
            Assert.AreEqual(expected, exception.Failure);
        }
    }
}
