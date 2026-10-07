using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    internal static class WeeklyEventFixture
    {
        internal static readonly GameEvent[] ExpectedEvents =
        [
            new EngineDevelopedEvent(PowerEnum.Steam, EngineDevelopmentKind.SpeedImproved, 40, 45),
            new EngineDevelopedEvent(PowerEnum.Electricity, EngineDevelopmentKind.SpeedImproved, 355, 360),
            new EngineDevelopedEvent(PowerEnum.Electricity, EngineDevelopmentKind.CostReduced, 360, 370),
            new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.SpeedImproved, 355, 360),
            new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.CostReduced, 360, 370),
            new EngineDevelopedEvent(PowerEnum.LinearMotor, EngineDevelopmentKind.SpeedImproved, 300, 310),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.BlockingSignal, 0, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.ConvertibleCross, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.AutoGate, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.CarTiltPendulum, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.RichCross, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.RetructableLong, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.DualSeat, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.MachineTilt, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.FreeGauge, 5, 5),
            new SpecialTechnologyDevelopedEvent(SpecialTechnology.DynamicSignal, 5, 10),
            new SteamAvailabilityEndedEvent(1881),
            new WarStartedEvent(1881, 1882, 120),
            new GoalsAchievedEvent(2300, 2300)
        ];

        internal static GameInfo SimultaneousEvents()
        {
            var game = Game();
            game.genkaiDenki = 355;
            game.genkaiKidosha = 355;
            game.genkaiLinear = 300;
            game.AccumulatedInvest = new InvestmentAmountAccumulated
            {
                steam = 5001, electricMotor = 1000000000, diesel = 1000000000,
                linearMotor = 4000000000, newPlan = 80000000
            };
            game.SteamYear = 1881;
            game.warModeList.Add(new GameInfo.WarMode { StartYear = 1881, EndYear = 1882, kamotsuIndex = 120 });
            game.SelectedMode.goalMoney = 1;
            YearEnd(game);
            return game;
        }
    }
}
