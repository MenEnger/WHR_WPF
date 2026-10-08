using System;
using System.Collections.Immutable;

namespace whr_wpf.Model
{
	public enum VehicleCreationReason
	{
		None,
		MissingName,
		EngineUnavailable,
		SpeedExceeded,
		SteamExpired,
		FreeGaugeUnavailable,
		SeatUnavailable,
		TiltUnavailable,
		NegativeSpeed,
		UndefinedPower,
		UndefinedGauge,
		UndefinedSeat,
		UndefinedTilt,
	}

	public enum CompositionCreationReason
	{
		None,
		MissingName,
		NoVehicles,
		GaugeMismatch,
		TrackTypeMismatch,
		PowerMismatch,
		TiltMismatch,
		SpeedTooLow,
		NegativeQuantity,
		QuantityExceeded,
		NegativeVehicleSpeed,
		UndefinedGauge,
		UndefinedTrackType,
		UndefinedPower,
		UndefinedSeat,
		UndefinedTilt,
		UnregisteredVehicle,
	}

	/// <summary>車両開発の判定理由と判定時点の値。</summary>
	public sealed record VehicleCreationCheck(VehicleCreationReason Reason, PowerEnum Power,
		int RequestedSpeed, int SpeedLimit, CarGaugeEnum Gauge, SeatEnum Seat, CarTiltEnum Tilt,
		int Year, int SteamEndYear)
	{
		public bool CanCreateVehicle => Reason == VehicleCreationReason.None;
	}

	/// <summary>編成作成の判定理由と判定段階で比較した値。</summary>
	public sealed record CompositionCreationCheck(CompositionCreationReason Reason)
	{
		public ImmutableArray<CarGaugeEnum> Gauges { get; init; } = ImmutableArray<CarGaugeEnum>.Empty;
		public ImmutableArray<RailTypeEnum> TrackTypes { get; init; } = ImmutableArray<RailTypeEnum>.Empty;
		public ImmutableArray<PowerEnum> Powers { get; init; } = ImmutableArray<PowerEnum>.Empty;
		public ImmutableArray<CarTiltEnum> Tilts { get; init; } = ImmutableArray<CarTiltEnum>.Empty;
		public int? ActualSpeed { get; init; }
		public int? MinimumSpeed { get; init; }
		public string CarName { get; init; }
		public int? RequestedQuantity { get; init; }
		public int? QuantityLimit { get; init; }
		public int? InvalidValue { get; init; }
		public bool CanCompositionMake => Reason == CompositionCreationReason.None;
	}

	/// <summary>車両開発の想定内の拒否。表示文はUI側で生成する。</summary>
	public sealed class VehicleDevelopmentRejectedException : InvalidOperationException
	{
		public VehicleDevelopmentRejectedException(VehicleCreationCheck check)
			: base("Vehicle development was rejected.")
		{
			Check = check;
		}

		public VehicleCreationCheck Check { get; }
	}

	/// <summary>編成作成の想定内の拒否。表示文はUI側で生成する。</summary>
	public sealed class CompositionCreationRejectedException : InvalidOperationException
	{
		public CompositionCreationRejectedException(CompositionCreationCheck check)
			: base("Composition creation was rejected.")
		{
			Check = check;
		}

		public CompositionCreationCheck Check { get; }
	}
}
