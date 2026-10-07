using System;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>車両・編成の判定理由を現在の画面の案内へ変換する。</summary>
    public static class CreationValidationFormatter
    {
        public static string Format(VehicleCreationCheck check) => check.Reason switch
        {
            VehicleCreationReason.None => "",
            VehicleCreationReason.MissingName => "名無しの権兵衛です",
            VehicleCreationReason.EngineUnavailable => check.Power switch
            {
                PowerEnum.LinearMotor => "リニアは作れません",
                PowerEnum.Electricity => "電車は作れません",
                PowerEnum.Diesel => "ディーゼルは作れません",
                _ => throw new ArgumentOutOfRangeException(nameof(check))
            },
            VehicleCreationReason.SpeedExceeded => "速すぎます",
            VehicleCreationReason.SteamExpired => "時代遅れです",
            VehicleCreationReason.FreeGaugeUnavailable => "フリーゲージは作れません",
            VehicleCreationReason.SeatUnavailable => check.Seat switch
            {
                SeatEnum.Dual => "デュアル不可",
                SeatEnum.Convertible => "転換式クロス不可",
                SeatEnum.RetructableLong => "収納式ロング不可",
                SeatEnum.Rich or SeatEnum.DoubleDeckerRich => "豪華クロス不可",
                _ => throw new ArgumentOutOfRangeException(nameof(check))
            },
            VehicleCreationReason.TiltUnavailable => check.Tilt switch
            {
                CarTiltEnum.Pendulum => "振り子式車体傾斜装置は未開発",
                CarTiltEnum.SimpleMecha or CarTiltEnum.HighMecha => "機械式式車体傾斜装置は未開発",
                _ => throw new ArgumentOutOfRangeException(nameof(check))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(check))
        };

        // 旧チェックAPIの成功時のnullは、表示側でのみ維持する。
        public static string Format(CompositionCreationCheck check) => check.Reason switch
        {
            CompositionCreationReason.None => null,
            CompositionCreationReason.MissingName => "名前が指定されていません",
            CompositionCreationReason.NoVehicles => "車両の指定がありません",
            CompositionCreationReason.GaugeMismatch => "車両の軌間に違いがあります",
            CompositionCreationReason.TrackTypeMismatch => "車両の軌道タイプに違いがあります",
            CompositionCreationReason.PowerMismatch => "車両の動力に違いがあります",
            CompositionCreationReason.TiltMismatch => "車両の車体傾斜装置に違いがあります",
            CompositionCreationReason.SpeedTooLow => "最高速度が40km/hに達していません",
            _ => throw new ArgumentOutOfRangeException(nameof(check))
        };

        // 検査時の詳細理由と、実行時の汎用案内の違いを維持する。
        public static string Format(VehicleDevelopmentRejectedException exception)
            => "車両を開発可能な技術が揃っていません";

        public static string Format(CompositionCreationRejectedException exception)
            => Format(exception.Check);
    }
}
