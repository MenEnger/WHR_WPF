using System;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>運行設定の拒否理由を現在の画面の案内へ変換する。</summary>
    public static class ServiceSettingFailureFormatter
    {
        public static string Format(ServiceSettingFailure failure) => (failure.Target, failure.Reason) switch
        {
            (ServiceSettingTarget.Direct, ServiceSettingRejectionReason.Unavailable) => "この路線には編成が設定できません",
            (ServiceSettingTarget.Through, ServiceSettingRejectionReason.Unavailable) => "未建設または休止中の区間には系統を設定できません",
            (ServiceSettingTarget.Through, ServiceSettingRejectionReason.FrequencyExceeded) => "この路線には編成が設定できません",
            (ServiceSettingTarget.Direct or ServiceSettingTarget.Through, ServiceSettingRejectionReason.StockUnavailable) => "編成が足りません",
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
    }
}
