using whr_wpf.View;
using whr_wpf.ViewModel;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class LineEquipmentFailureFormatterTests
    {
        [DataTestMethod]
        [DataRow(LineEquipmentRejectionReason.SpeedUpUnavailable, "この路線はスピードアップできません")]
        [DataRow(LineEquipmentRejectionReason.UnElectrifyUnavailable, "この路線は非電化できません")]
        [DataRow(LineEquipmentRejectionReason.ElectrifyUnavailable, "この路線は電化できません")]
        [DataRow(LineEquipmentRejectionReason.NarrowGaugeUnavailable, "この路線は狭軌に変更できません")]
        [DataRow(LineEquipmentRejectionReason.ExpanseGaugeUnavailable, "この路線は標準軌に変更できません")]
        [DataRow(LineEquipmentRejectionReason.AddLaneUnavailable, "この路線は増設できません")]
        [DataRow(LineEquipmentRejectionReason.ReduceUnavailable, "この路線は削減できません")]
        [DataRow(LineEquipmentRejectionReason.TaihiUnavailable, "この路線は待避線を設定できません")]
        public void MessagesKeepExistingText(LineEquipmentRejectionReason reason, string expected)
            => Assert.AreEqual(expected, LineEquipmentFailureFormatter.Format(new(reason, "試験線", false)));

        [TestMethod]
        public void UnknownReasonIsNotSilentlyAccepted()
            => Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                LineEquipmentFailureFormatter.Format(new((LineEquipmentRejectionReason)999, "試験線", false)));

        [TestMethod]
        public void ReformViewModelPreservesAvailabilityAndPropagatesStructuredRefusal()
        {
            var game = Game();
            var line = Line(game);
            var vm = new ReformViewModel(null!, line, game);
            Assert.IsTrue(vm.SpeedUp.CanExecute(null));
            line.bestSpeedUpKaisu = 5;
            Assert.IsFalse(vm.SpeedUp.CanExecute(null));
            var exception = Assert.ThrowsException<LineEquipmentRejectedException>(vm.ExecuteSpeedUp);
            Assert.AreEqual(LineEquipmentRejectionReason.SpeedUpUnavailable, exception.Failure.Reason);
            Assert.AreEqual("この路線はスピードアップできません", LineEquipmentFailureFormatter.Format(exception.Failure));
            Assert.AreEqual((60, 5, 1000000L), (line.bestSpeed, line.bestSpeedUpKaisu, game.Money));
        }
    }
}
