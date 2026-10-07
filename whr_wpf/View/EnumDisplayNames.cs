using System;
using System.ComponentModel.DataAnnotations;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>
    /// ゲームenumの表示名。計算に使う属性はモデル側で管理する。
    /// </summary>
    public static class EnumDisplayNames
    {
        public static string ToName(this Enum value)
        {
            return value switch
            {
                DifficultyLevelEnum.VeryEasy => "超簡単",
                DifficultyLevelEnum.Easy => "簡単",
                DifficultyLevelEnum.Normal => "普通",
                DifficultyLevelEnum.Hard => "難しい",
                DifficultyLevelEnum.VeryHard => "激むず",
                CarGaugeEnum.Narrow => "狭軌",
                CarGaugeEnum.Regular => "標準軌",
                CarGaugeEnum.FreeGauge => "フリーゲージ",
                PowerEnum.Steam => "蒸気",
                PowerEnum.Electricity => "電気",
                PowerEnum.Diesel => "ディーゼル",
                PowerEnum.LinearMotor => "リニアモーター",
                CarTiltEnum.None => "なし",
                CarTiltEnum.Pendulum => "振り子式",
                CarTiltEnum.SimpleMecha => "簡易機械式",
                CarTiltEnum.HighMecha => "高性能機械式",
                SeatEnum.None => "座席なし",
                SeatEnum.RetructableLong => "収容式ロングシート",
                SeatEnum.Long => "ロングシート",
                SeatEnum.Dual => "デュアルシート",
                SeatEnum.Semi => "セミクロスシート",
                SeatEnum.Convertible => "転換クロスシート",
                SeatEnum.DoubleDeckerRotatable => "二階建て回転クロス",
                SeatEnum.Rotatable => "回転クロスシート",
                SeatEnum.DoubleDeckerRich => "二階建て豪華クロス",
                SeatEnum.Rich => "豪華クロスシート",
                TaihisenEnum.None => "なし",
                TaihisenEnum.Every100km => "100kmごと",
                TaihisenEnum.Every50km => "50kmごと",
                TaihisenEnum.Every20km => "20kmごと",
                TaihisenEnum.Every10km => "10kmごと",
                TaihisenEnum.Every5km => "5kmごと",
                TaihisenEnum.Every2km => "2kmごと",
                DiagramType.None => "ダイヤなし",
                DiagramType.Regular => "普通ダイヤ",
                DiagramType.LimittedExpressPrior => "特急優先ダイヤ",
                DiagramType.OverCrowded => "過密ダイヤ",
                DiagramType.Parallel => "並行ダイヤ",
                LineGrade.MostImportant => "最重要幹線",
                LineGrade.Main => "幹線",
                LineGrade.Local => "地方線",
                LinePropertyType.JapaneseInterCity => "普通線",
                LinePropertyType.Surburb => "郊外線",
                LinePropertyType.Outskirts => "近郊線",
                LinePropertyType.Plain => "平野線",
                LinePropertyType.Mountain => "山地線",
                LinePropertyType.Alpine => "山脈線",
                LinePropertyType.Sea => "海線",
                LinePropertyType.RussianPlain => "ロシア平野線",
                LinePropertyType.Underground => "地下線",
                InvestmentAmountEnum.Nothing => "なし",
                InvestmentAmountEnum.MN2000 => "2000万円",
                InvestmentAmountEnum.MN5000 => "5000万円",
                InvestmentAmountEnum.OK1 => "1億円",
                InvestmentAmountEnum.OK5 => "5億円",
                InvestmentAmountEnum.OK10 => "10億円",
                InvestmentAmountEnum.OK25 => "25億円",
                InvestmentAmountEnum.OK50 => "50億円",
                InvestmentAmountEnum.OK100 => "100億円",
                InvestmentAmountLinearEnum.Nothing => "なし",
                InvestmentAmountLinearEnum.OK10 => "10億円",
                InvestmentAmountLinearEnum.OK25 => "25億円",
                InvestmentAmountLinearEnum.OK50 => "50億円",
                InvestmentAmountLinearEnum.OK100 => "100億円",
                InvestmentAmountLinearEnum.OK250 => "250億円",
                InvestmentAmountLinearEnum.OK500 => "500億円",
                LineGoalTargetEnum.MostImportant => "最重要幹線",
                LineGoalTargetEnum.MostImportantAndMain => "最重要幹線及び幹線",
                LineGoalTargetEnum.All => "全線",
                _ => AttributeName(value)
            };
        }

        private static string AttributeName(Enum value)
        {
            // 外部enumと表示名のない定義値には従来の属性読取を使う。
            DisplayAttribute attribute = value.GetAttribute<DisplayAttribute>();
            return attribute == null ? value.ToString() : attribute.Name;
        }
    }
}
