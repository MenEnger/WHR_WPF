using whr_wpf.ViewModel.Component;
using static whr_wpf.ViewModel.ConstructWindowViewModel;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    [TestCategory("Selection")]
    public class SelectionValueEqualityTests
    {
        [TestMethod]
        public void TaihiEqualityUsesEnumAndIgnoresCaption()
        {
            AssertValueEquality(
                new TaihiViewComponent { Enum = TaihisenEnum.Every20km, Caption = "同じ表示" },
                new TaihiViewComponent { Enum = TaihisenEnum.Every20km, Caption = "別表示" },
                new TaihiViewComponent { Enum = TaihisenEnum.Every2km, Caption = "同じ表示" },
                item => item.Caption = "変更後");
        }

        [TestMethod]
        public void LaneEqualityUsesCountAndIgnoresCaption()
        {
            AssertValueEquality(
                new LaneNumViewModel { LaneSu = 2, Caption = "同じ表示" },
                new LaneNumViewModel { LaneSu = 2, Caption = "別表示" },
                new LaneNumViewModel { LaneSu = 1, Caption = "同じ表示" },
                item => item.Caption = "変更後");
        }

        [TestMethod]
        public void RailEqualityUsesAllSettingsAndIgnoresCaption()
        {
            AssertValueEquality(
                new RailTypeViewModel { RailType = RailTypeEnum.Iron, RailGauge = RailGaugeEnum.Narrow, IsElectrified = true, Caption = "同じ表示" },
                new RailTypeViewModel { RailType = RailTypeEnum.Iron, RailGauge = RailGaugeEnum.Narrow, IsElectrified = true, Caption = "別表示" },
                new RailTypeViewModel { RailType = RailTypeEnum.LinearMotor, RailGauge = RailGaugeEnum.Narrow, IsElectrified = true, Caption = "同じ表示" },
                item => item.Caption = "変更後");
        }

        [TestMethod]
        public void RailNullableGaugeAndElectrificationEachParticipateInEquality()
        {
            var first = new RailTypeViewModel { RailType = RailTypeEnum.Iron, RailGauge = RailGaugeEnum.Narrow, IsElectrified = true, Caption = "同じ表示" };
            var other = new RailTypeViewModel { RailType = RailTypeEnum.Iron, RailGauge = RailGaugeEnum.Regular, IsElectrified = true, Caption = "同じ表示" };
            Assert.IsFalse(first.Equals(other));
            other.RailGauge = null;
            Assert.IsFalse(first.Equals(other));
            first.RailGauge = null;
            Assert.IsTrue(first.Equals(other));
            Assert.IsTrue(((object)first).Equals(other));
            Assert.AreEqual(first.GetHashCode(), other.GetHashCode());
            other.IsElectrified = false;
            Assert.IsFalse(first.Equals(other));
            Assert.IsFalse(((object)first).Equals(other));
        }

        [TestMethod]
        public void LinearRailStillComparesGaugeAndElectrificationValues()
        {
            var first = new RailTypeViewModel { RailType = RailTypeEnum.LinearMotor, RailGauge = null, IsElectrified = true };
            var other = new RailTypeViewModel { RailType = RailTypeEnum.LinearMotor, RailGauge = RailGaugeEnum.Narrow, IsElectrified = true };
            // リニア選択肢でも保存された軌間・電化を省略して同一扱いにしない。
            Assert.IsFalse(first.Equals(other));
            other.RailGauge = null;
            Assert.IsTrue(first.Equals(other));
            Assert.AreEqual(first.GetHashCode(), other.GetHashCode());
            other.IsElectrified = false;
            Assert.IsFalse(first.Equals(other));
        }

        private static void AssertValueEquality<T>(T first, T sameValuesDifferentCaption, T differentValuesSameCaption, Action<T> changeCaption)
            where T : class, IEquatable<T>
        {
            Assert.IsTrue(first.Equals(sameValuesDifferentCaption));
            Assert.IsTrue(sameValuesDifferentCaption.Equals(first));
            Assert.IsTrue(((object)first).Equals(sameValuesDifferentCaption));
            Assert.IsTrue(((object)sameValuesDifferentCaption).Equals(first));
            Assert.IsFalse(first.Equals(differentValuesSameCaption));
            Assert.IsFalse(differentValuesSameCaption.Equals(first));
            Assert.IsFalse(((object)first).Equals(differentValuesSameCaption));
            Assert.IsTrue(first.Equals(first));
            Assert.IsFalse(first.Equals((T?)null));
            Assert.IsTrue(((object)first).Equals(first));
            Assert.IsFalse(((object)first).Equals(null));
            Assert.IsFalse(((object)first).Equals(new object()));
            int originalHash = first.GetHashCode();
            Assert.AreEqual(originalHash, sameValuesDifferentCaption.GetHashCode());
            changeCaption(first);
            Assert.IsTrue(first.Equals(sameValuesDifferentCaption));
            Assert.IsTrue(((object)first).Equals(sameValuesDifferentCaption));
            Assert.AreEqual(originalHash, first.GetHashCode());
            Assert.AreEqual(first.GetHashCode(), sameValuesDifferentCaption.GetHashCode());
        }
    }
}
