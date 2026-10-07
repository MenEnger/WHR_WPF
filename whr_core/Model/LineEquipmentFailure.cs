using System;

namespace whr_wpf.Model
{
	public enum LineEquipmentRejectionReason
	{
		SpeedUpUnavailable,
		UnElectrifyUnavailable,
		ElectrifyUnavailable,
		NarrowGaugeUnavailable,
		ExpanseGaugeUnavailable,
		AddLaneUnavailable,
		ReduceUnavailable,
		TaihiUnavailable,
	}

	/// <summary>設備操作の拒否理由と判定時点の路線状態。</summary>
	public sealed record LineEquipmentFailure(LineEquipmentRejectionReason Reason, string LineName, bool IsExist)
	{
		public int? BestSpeed { get; init; }
		public int? ImprovementCount { get; init; }
		public int? LaneCount { get; init; }
		public RailTypeEnum? Type { get; init; }
		public RailGaugeEnum? Gauge { get; init; }
		public bool? IsElectrified { get; init; }
	}

	/// <summary>設備操作の想定内の拒否。表示文はUI側で生成する。</summary>
	public sealed class LineEquipmentRejectedException : InvalidOperationException
	{
		public LineEquipmentRejectedException(LineEquipmentFailure failure)
			: base("Line equipment operation was rejected.")
		{
			Failure = failure;
		}

		public LineEquipmentFailure Failure { get; }
	}
}
