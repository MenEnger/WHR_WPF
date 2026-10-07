using System;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>編成の数量不足を現在の画面の案内へ変換する。</summary>
    public static class StockShortageFormatter
    {
        public static string Format(StockShortageFailure failure) => failure.Operation switch
        {
            StockShortageOperation.Use => $"編成数量が{failure.MissingQuantity}つ不足しています",
            StockShortageOperation.Sale => "保有編成数が売却数に対して不足しています",
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
    }
}
