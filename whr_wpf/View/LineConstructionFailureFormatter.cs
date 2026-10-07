using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>建造設定の拒否を現在の画面の案内へ変換する。</summary>
    public static class LineConstructionFailureFormatter
    {
        public static string Format(LineConstructionFailure failure) => "与えられた引数では路線を建造できません";
    }
}
