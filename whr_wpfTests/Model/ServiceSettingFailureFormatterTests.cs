using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class ServiceSettingFailureFormatterTests
    {
        [DataTestMethod]
        [DataRow(ServiceSettingTarget.Direct, ServiceSettingRejectionReason.Unavailable, "この路線には編成が設定できません")]
        [DataRow(ServiceSettingTarget.Through, ServiceSettingRejectionReason.Unavailable, "未建設または休止中の区間には系統を設定できません")]
        [DataRow(ServiceSettingTarget.Through, ServiceSettingRejectionReason.FrequencyExceeded, "この路線には編成が設定できません")]
        [DataRow(ServiceSettingTarget.Direct, ServiceSettingRejectionReason.StockUnavailable, "編成が足りません")]
        [DataRow(ServiceSettingTarget.Through, ServiceSettingRejectionReason.StockUnavailable, "編成が足りません")]
        public void MessagesKeepExistingText(ServiceSettingTarget target, ServiceSettingRejectionReason reason, string expected)
            => Assert.AreEqual(expected, ServiceSettingFailureFormatter.Format(new(target, reason, "対象", 60)));

        [DataTestMethod]
        [DataRow(ServiceSettingTarget.Direct, ServiceSettingRejectionReason.FrequencyExceeded)]
        [DataRow((ServiceSettingTarget)999, ServiceSettingRejectionReason.StockUnavailable)]
        [DataRow(ServiceSettingTarget.Through, (ServiceSettingRejectionReason)999)]
        public void UnsupportedCombinationIsNotSilentlyDisplayed(ServiceSettingTarget target, ServiceSettingRejectionReason reason)
            => Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                ServiceSettingFailureFormatter.Format(new(target, reason, "対象", 60)));
    }
}
