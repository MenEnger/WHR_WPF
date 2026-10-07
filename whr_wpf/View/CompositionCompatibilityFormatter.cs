using System;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>編成適合の拒否理由を現在の画面の案内へ変換する。</summary>
    public static class CompositionCompatibilityFormatter
    {
        public static string Format(CompositionCompatibilityFailure failure) => failure.Reason switch
        {
            CompositionCompatibilityReason.RequiresLinearVehicle => "車両がリニアではありません",
            CompositionCompatibilityReason.RequiresLinearTrack => "リニア軌道ではありません",
            CompositionCompatibilityReason.GaugeMismatch => "路線幅が違います",
            CompositionCompatibilityReason.RequiresElectrification => "路線が非電化です",
            CompositionCompatibilityReason.SteamExpired => "蒸気機関車は時代遅れで使えません",
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
    }
}
