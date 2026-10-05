using System.Reflection;

namespace whr_wpf.Model.Tests
{
    // 現状の状態変更を記録するための小さなシナリオ。原作への適合は判定しない。
    internal static class CurrentBehaviorFixture
    {
        internal static GameInfo Game()
        {
            var game = new GameInfo
            {
                Difficulty = DifficultyLevelEnum.Normal, BasicYear = 1880, SteamYear = 1970,
                TechCost = 100, LineMakeCost = 100, Rpm = 100, FarePerKm = 10,
                Season = SeasonEnum.Constantly, longwayList = new List<Longway>(),
                warModeList = new List<GameInfo.WarMode>(),
                stations = new List<Station> { Town("A", 100), Town("B", 150) }
            };
            game.SelectedMode = new Mode
            {
                Year = 1880, MYear = 2300, Money = 1000000,
                DefautltCompositions = new List<DefautltComposition>(),
                LineSettings = new List<Mode.LineDefaultSetting>(),
                KeitoDefaultSettings = new List<Mode.KeitoDefaultSetting>()
            };
            // 本体を変えずに乱数で決まる景気の初期位相だけ固定する。
            // 貨物単価の乱数を含むテストでは金額の範囲と収支の整合性を確認する。
            typeof(GameInfo).GetField("economyTrends", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(game, new double[] { 90, 90, 90, 90 });
            return game;
        }

        internal static Station Town(string name, int population)
            => new Station { Name = name, Population = population, Size = StationSize.Other };

        internal static DefautltComposition Stock(GameInfo game, int units = 10)
        {
            var stock = new DefautltComposition
            {
                Name = "試験編成", BestSpeed = 60, CarCount = 8, Price = 100,
                Power = PowerEnum.Steam, Type = RailTypeEnum.Iron, Gauge = CarGaugeEnum.Narrow,
                seat = SeatEnum.Semi, Tilt = CarTiltEnum.None
            };
            stock.Purchase(game, units);
            game.compositions.Add(stock);
            return stock;
        }

        internal static Line Line(GameInfo game, bool exists = true)
        {
            var line = new Line
            {
                Name = "試験線", Start = game.stations[0], End = game.stations[1],
                Distance = 9, IsExist = exists, Type = RailTypeEnum.Iron,
                IsElectrified = false, gauge = RailGaugeEnum.Narrow, bestSpeed = 60,
                LaneNum = 2, taihisen = TaihisenEnum.Every20km,
                diagram = DiagramType.LimittedExpressPrior, grade = LineGrade.Main
            };
            game.lines.Add(line);
            line.Start.BelongingLines.Add(line);
            line.End.BelongingLines.Add(line);
            return line;
        }

        internal static KeitoDiagram Through(GameInfo game, params Line[] route)
        {
            var through = new KeitoDiagram
            {
                Name = "試験系統", start = route[0].Start, end = route[^1].End,
                route = route.ToList()
            };
            foreach (var line in route) line.belongingKeitoDiagrams.Add(through);
            game.diagrams.Add(through);
            return through;
        }

        internal static void YearEnd(GameInfo game)
        {
            game.Month = 12;
            game.Week = 4;
        }

        internal static int DevelopedMask(GameInfo game) => new[]
        {
            game.isDevelopedBlockingSignal, game.isDevelopedConvertibleCross,
            game.isDevelopedAutoGate, game.isDevelopedCarTiltPendulum, game.isDevelopedRichCross,
            game.isDevelopedRetructableLong, game.isDevelopedDualSeat, game.isDevelopedMachineTilt,
            game.isDevelopedFreeGauge, game.isDevelopedDynamicSignal
        }.Select((value, index) => value ? 1 << index : 0).Sum();

        internal static int DevelopedCount(GameInfo game)
            => (int)System.Numerics.BitOperations.PopCount((uint)DevelopedMask(game));
    }
}
