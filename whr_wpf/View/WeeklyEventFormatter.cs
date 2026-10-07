using System;
using System.Collections.Generic;
using System.Linq;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>週次イベントを、現在のUIで使う案内へ変換する。</summary>
    public static class WeeklyEventFormatter
    {
        public static string FormatMany(IEnumerable<GameEvent> events)
            => string.Join("\n===\n", events.Select(Format));

        public static string Format(GameEvent gameEvent) => gameEvent switch
        {
            EngineDevelopedEvent engine => FormatEngine(engine),
            SpecialTechnologyDevelopedEvent technology => FormatTechnology(technology.Technology),
            SteamAvailabilityEndedEvent => "今年から、蒸気機関車の設定が不可能になります。\n（現在設定中のものは引き続き使用可能です）",
            WarStartedEvent => "今年より戦時体制に突入します。\n貨物取扱量が変化し、貨物輸送を削減することができなくなります。",
            WarEndedEvent => "戦時体制は終了しました",
            GoalsAchievedEvent => "おめでとうございます！\n目標を達成しました。フリーモードに移行しました。",
            _ => throw new ArgumentOutOfRangeException(nameof(gameEvent))
        };

        private static string FormatEngine(EngineDevelopedEvent engine)
            => (engine.Power, engine.Kind) switch
            {
                (PowerEnum.Steam, EngineDevelopmentKind.SpeedImproved) => $"蒸気機関の改良が完了しました\n蒸気機関車の開発可能速度が{engine.CurrentLevel}km/hになります。",
                (PowerEnum.Electricity, EngineDevelopmentKind.Available) => "電気モーターが完成しました\n電車が作成できるようになります",
                (PowerEnum.Electricity, EngineDevelopmentKind.SpeedImproved) => $"電気モーターが改良されました\n電車の開発可能速度が{engine.CurrentLevel}km/hになります",
                (PowerEnum.Electricity, EngineDevelopmentKind.CostReduced) => "電気モーターが改良されました\n電車の作成コストが下がります",
                (PowerEnum.Diesel, EngineDevelopmentKind.Available) => "ディーゼル機関が完成しました\nディーゼルカーが作成できるようになります",
                (PowerEnum.Diesel, EngineDevelopmentKind.SpeedImproved) => $"ディーゼル機関が改良されました\nディーゼルカーの開発可能速度が{engine.CurrentLevel}km/hになります",
                (PowerEnum.Diesel, EngineDevelopmentKind.CostReduced) => "ディーゼル機関が改良されました\nディーゼルカーの作成コストが下がります",
                (PowerEnum.LinearMotor, EngineDevelopmentKind.Available) => "リニアが完成しました\nリニアカーが作成できるようになります",
                (PowerEnum.LinearMotor, EngineDevelopmentKind.SpeedImproved) => $"リニアが改良されました\nリニアの開発可能速度が{engine.CurrentLevel}km/hになります",
                _ => throw new ArgumentOutOfRangeException(nameof(engine))
            };

        private static string FormatTechnology(SpecialTechnology technology) => technology switch
        {
            SpecialTechnology.BlockingSignal => "閉塞信号が完成しました\n運行可能数が増加します",
            SpecialTechnology.ConvertibleCross => "転換クロスシートが完成しました\n通勤列車にも使えるクロスシートです",
            SpecialTechnology.AutoGate => "自動改札機が完成しました\n客一人当たりのコストが下がります",
            SpecialTechnology.CarTiltPendulum => "振子式車体傾斜装置が完成しました\n対応車では、路線最高速度を20%上越えることができます",
            SpecialTechnology.RichCross => "豪華クロスシートが完成しました\n最高の乗り心地を保障する座席です",
            SpecialTechnology.RetructableLong => "収納式ロングシートが完成しました\n普通のロングシートよりも定員数が多くなります",
            SpecialTechnology.DualSeat => "デュアルシートが完成しました\n乗り心地と定員数を両立させた座席です",
            SpecialTechnology.MachineTilt => "機械式車体傾斜装置が完成しました\n①簡易タイプでは、振子式と同性能で価格が安くなります。\n②高性能タイプは、路線最高速度を33%越えることができます。",
            SpecialTechnology.FreeGauge => "フリーゲージトレインが完成しました\nフリーゲージトレインは、狭軌・標準軌関係なく走ることができます",
            SpecialTechnology.DynamicSignal => "移動閉塞信号が完成しました。\n運行可能数が増加します",
            _ => throw new ArgumentOutOfRangeException(nameof(technology))
        };
    }
}
