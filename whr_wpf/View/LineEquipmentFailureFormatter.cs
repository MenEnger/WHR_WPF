using System;
using whr_wpf.Model;

namespace whr_wpf.View
{
    /// <summary>路線設備の操作拒否を現在の画面の案内へ変換する。</summary>
    public static class LineEquipmentFailureFormatter
    {
        public static string Format(LineEquipmentFailure failure) => failure.Reason switch
        {
            LineEquipmentRejectionReason.SpeedUpUnavailable => "この路線はスピードアップできません",
            LineEquipmentRejectionReason.UnElectrifyUnavailable => "この路線は非電化できません",
            LineEquipmentRejectionReason.ElectrifyUnavailable => "この路線は電化できません",
            LineEquipmentRejectionReason.NarrowGaugeUnavailable => "この路線は狭軌に変更できません",
            LineEquipmentRejectionReason.ExpanseGaugeUnavailable => "この路線は標準軌に変更できません",
            LineEquipmentRejectionReason.AddLaneUnavailable => "この路線は増設できません",
            LineEquipmentRejectionReason.ReduceUnavailable => "この路線は削減できません",
            LineEquipmentRejectionReason.TaihiUnavailable => "この路線は待避線を設定できません",
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
    }
}
