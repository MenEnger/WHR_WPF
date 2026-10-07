using System;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>資金不足とゲーム終了を現在の画面の案内へ変換する。</summary>
    public static class GameFailureFormatter
    {
        public static string Format(MoneyShortageFailure failure) => "お金が足りません";

        public static string Format(GameOverFailure failure) => failure.Reason switch
        {
            GameOverReason.DeadlineExceeded => "目標の達成に失敗しました。ゲームオーバーです。",
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
    }
}
