using System;

namespace whr_wpf.Model
{
	public enum ServiceSettingTarget
	{
		Direct,
		Through,
	}

	public enum ServiceSettingRejectionReason
	{
		Unavailable,
		FrequencyExceeded,
		StockUnavailable,
	}

	/// <summary>運行設定の拒否理由と既存の評価順で取得した値。</summary>
	public sealed record ServiceSettingFailure(ServiceSettingTarget Target, ServiceSettingRejectionReason Reason,
		string TargetName, int RequestedRunningPerDay)
	{
		public int? RequiredUnits { get; init; }
		public int? AvailableUnits { get; init; }
		public int? CalculatedMissingUnits { get; init; }
	}

	/// <summary>運行設定の想定内の拒否。表示文はUI側で生成する。</summary>
	public sealed class ServiceSettingRejectedException : InvalidOperationException
	{
		public ServiceSettingRejectedException(ServiceSettingFailure failure)
			: base("Service setting was rejected.")
		{
			Failure = failure;
		}

		public ServiceSettingFailure Failure { get; }
	}
}
