namespace whr_wpf.Model
{
	public enum CompositionCompatibilityReason
	{
		RequiresLinearVehicle,
		RequiresLinearTrack,
		GaugeMismatch,
		RequiresElectrification,
		SteamExpired,
	}

	/// <summary>編成適合の拒否理由と判定時点の値。</summary>
	public sealed record CompositionCompatibilityFailure(CompositionCompatibilityReason Reason, string LineName)
	{
		public RailTypeEnum? LineType { get; init; }
		public RailTypeEnum? CompositionType { get; init; }
		public RailGaugeEnum? LineGauge { get; init; }
		public CarGaugeEnum? CompositionGauge { get; init; }
		public bool? LineElectrified { get; init; }
		public bool? CompositionElectrified { get; init; }
		public int? Year { get; init; }
		public int? SteamEndYear { get; init; }
	}
}
