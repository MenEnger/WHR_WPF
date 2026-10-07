using System.ComponentModel.DataAnnotations;
using whr_wpf.Model;

namespace whr_wpf.View.Tests
{
    [TestClass]
    [TestCategory("Display")]
    public class EnumDisplayNameTests
    {
        // 期待文は製品の属性から生成せず、表示契約を独立した対応表として保存する。
        private static readonly Dictionary<Enum, string> ExpectedNames = new()
        {
            [DifficultyLevelEnum.VeryEasy] = "超簡単",
            [DifficultyLevelEnum.Easy] = "簡単",
            [DifficultyLevelEnum.Normal] = "普通",
            [DifficultyLevelEnum.Hard] = "難しい",
            [DifficultyLevelEnum.VeryHard] = "激むず",
            [CarGaugeEnum.Narrow] = "狭軌",
            [CarGaugeEnum.Regular] = "標準軌",
            [CarGaugeEnum.FreeGauge] = "フリーゲージ",
            [PowerEnum.Steam] = "蒸気",
            [PowerEnum.Electricity] = "電気",
            [PowerEnum.Diesel] = "ディーゼル",
            [PowerEnum.LinearMotor] = "リニアモーター",
            [CarTiltEnum.None] = "なし",
            [CarTiltEnum.Pendulum] = "振り子式",
            [CarTiltEnum.SimpleMecha] = "簡易機械式",
            [CarTiltEnum.HighMecha] = "高性能機械式",
            [SeatEnum.None] = "座席なし",
            [SeatEnum.RetructableLong] = "収容式ロングシート",
            [SeatEnum.Long] = "ロングシート",
            [SeatEnum.Dual] = "デュアルシート",
            [SeatEnum.Semi] = "セミクロスシート",
            [SeatEnum.Convertible] = "転換クロスシート",
            [SeatEnum.DoubleDeckerRotatable] = "二階建て回転クロス",
            [SeatEnum.Rotatable] = "回転クロスシート",
            [SeatEnum.DoubleDeckerRich] = "二階建て豪華クロス",
            [SeatEnum.Rich] = "豪華クロスシート",
            [TaihisenEnum.None] = "なし",
            [TaihisenEnum.Every100km] = "100kmごと",
            [TaihisenEnum.Every50km] = "50kmごと",
            [TaihisenEnum.Every20km] = "20kmごと",
            [TaihisenEnum.Every10km] = "10kmごと",
            [TaihisenEnum.Every5km] = "5kmごと",
            [TaihisenEnum.Every2km] = "2kmごと",
            [DiagramType.None] = "ダイヤなし",
            [DiagramType.Regular] = "普通ダイヤ",
            [DiagramType.LimittedExpressPrior] = "特急優先ダイヤ",
            [DiagramType.OverCrowded] = "過密ダイヤ",
            [DiagramType.Parallel] = "並行ダイヤ",
            [LineGrade.MostImportant] = "最重要幹線",
            [LineGrade.Main] = "幹線",
            [LineGrade.Local] = "地方線",
            [LinePropertyType.JapaneseInterCity] = "普通線",
            [LinePropertyType.Surburb] = "郊外線",
            [LinePropertyType.Outskirts] = "近郊線",
            [LinePropertyType.Plain] = "平野線",
            [LinePropertyType.Mountain] = "山地線",
            [LinePropertyType.Alpine] = "山脈線",
            [LinePropertyType.Sea] = "海線",
            [LinePropertyType.RussianPlain] = "ロシア平野線",
            [LinePropertyType.Underground] = "地下線",
            [InvestmentAmountEnum.Nothing] = "なし",
            [InvestmentAmountEnum.MN2000] = "2000万円",
            [InvestmentAmountEnum.MN5000] = "5000万円",
            [InvestmentAmountEnum.OK1] = "1億円",
            [InvestmentAmountEnum.OK5] = "5億円",
            [InvestmentAmountEnum.OK10] = "10億円",
            [InvestmentAmountEnum.OK25] = "25億円",
            [InvestmentAmountEnum.OK50] = "50億円",
            [InvestmentAmountEnum.OK100] = "100億円",
            [InvestmentAmountLinearEnum.Nothing] = "なし",
            [InvestmentAmountLinearEnum.OK10] = "10億円",
            [InvestmentAmountLinearEnum.OK25] = "25億円",
            [InvestmentAmountLinearEnum.OK50] = "50億円",
            [InvestmentAmountLinearEnum.OK100] = "100億円",
            [InvestmentAmountLinearEnum.OK250] = "250億円",
            [InvestmentAmountLinearEnum.OK500] = "500億円",
            [LineGoalTargetEnum.MostImportant] = "最重要幹線",
            [LineGoalTargetEnum.MostImportantAndMain] = "最重要幹線及び幹線",
            [LineGoalTargetEnum.All] = "全線"
        };

        [TestMethod]
        public void AllSixtyNineDisplayNamesMatchIndependentLiteralContract()
        {
            Assert.AreEqual(69, ExpectedNames.Count);
            foreach (var pair in ExpectedNames)
                Assert.AreEqual(pair.Value, pair.Key.ToName(), $"{pair.Key.GetType().Name}.{pair.Key}");
            // 各表示対象enumの定義値が対応表に欠けていないことも確認する。
            foreach (var type in ExpectedNames.Keys.Select(value => value.GetType()).Distinct())
                foreach (Enum value in Enum.GetValues(type))
                    Assert.IsTrue(ExpectedNames.ContainsKey(value), $"表示契約が未記録: {type.Name}.{value}");
        }

        [DataTestMethod]
        [DataRow(SeasonEnum.Constantly, "Constantly")]
        [DataRow(KamotsuEnum.Nothing, "Nothing")]
        [DataRow(InfoPosiEnum.TopLeft, "TopLeft")]
        [DataRow(RailTypeEnum.Iron, "Iron")]
        [DataRow(RailGaugeEnum.Narrow, "Narrow")]
        [DataRow(StationSize.Other, "Other")]
        public void DefinedValueWithoutDisplayNameFallsBackToEnumName(Enum value, string expected)
            => Assert.AreEqual(expected, value.ToName());

        [TestMethod]
        public void SameNumericValueInDifferentEnumTypesKeepsItsOwnName()
        {
            Assert.AreEqual(1, (int)PowerEnum.Electricity);
            Assert.AreEqual(1, (int)DifficultyLevelEnum.VeryEasy);
            Assert.AreEqual("電気", PowerEnum.Electricity.ToName());
            Assert.AreEqual("超簡単", DifficultyLevelEnum.VeryEasy.ToName());
            Assert.AreEqual("電気", PowerEnum.Electricity.ToName());
        }

        [TestMethod]
        public void UndefinedValueAndNullKeepExistingInternalExceptions()
        {
            Assert.ThrowsException<IndexOutOfRangeException>(() => ((PowerEnum)999).ToName());
            Assert.ThrowsException<NullReferenceException>(() => ((Enum)null!).ToName());
        }

        [TestMethod]
        public void ExternalEnumKeepsDisplayAttributeAndUnattributedFallback()
        {
            Assert.AreEqual("外部表示名", ExternalEnum.Named.ToName());
            Assert.AreEqual("Unnamed", ExternalEnum.Unnamed.ToName());
        }

        [TestMethod]
        public void ExternalDisplayAttributeWithoutNameKeepsNullRatherThanEnumFallback()
            => Assert.IsNull(ExternalEnum.NullName.ToName());

        private enum ExternalEnum
        {
            [Display(Name = "外部表示名")]
            Named,
            Unnamed,
            [Display]
            NullName
        }
    }
}
