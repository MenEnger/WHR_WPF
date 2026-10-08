using System;

namespace whr_wpf.Model
{
	public enum TechnologyInvestmentDepartment
	{
		Steam,
		Electric,
		Diesel,
		Linear,
		NewPlan,
	}

	/// <summary>投資不可の部門と、拒否した設定額。モデル変更後も判定時点の値を保持する。</summary>
	public sealed record TechnologyInvestmentFailure(TechnologyInvestmentDepartment Department, int RequestedAmount);

	/// <summary>投資不可部門への非0設定を拒否する。</summary>
	public sealed class TechnologyInvestmentRejectedException : InvalidOperationException
	{
		public TechnologyInvestmentRejectedException(TechnologyInvestmentFailure failure)
			: base("Technology investment is unavailable.")
		{
			Failure = failure;
		}

		public TechnologyInvestmentFailure Failure { get; }
	}
}
