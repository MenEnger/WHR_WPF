using System;

namespace whr_wpf.Model
{
	/// <summary>建造拒否の判定対象となった入力設定。</summary>
	public sealed record LineConstructionFailure(string LineName, int RequestedSpeed,
		RailTypeEnum RequestedType, bool? RequestedElectrification,
		RailGaugeEnum? RequestedGauge, int RequestedLaneCount);

	/// <summary>路線建造の想定内の拒否。表示文はUI側で生成する。</summary>
	public sealed class LineConstructionRejectedException : InvalidOperationException
	{
		public LineConstructionRejectedException(LineConstructionFailure failure)
			: base("Line construction was rejected.")
		{
			Failure = failure;
		}

		public LineConstructionFailure Failure { get; }
	}
}
