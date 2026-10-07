using System;

namespace whr_wpf.Model
{
	public enum StockShortageOperation
	{
		Use,
		Sale,
	}

	/// <summary>編成の数量不足と判定時点の要求数・余剰数。</summary>
	public sealed record StockShortageFailure(StockShortageOperation Operation,
		int RequestedQuantity, int AvailableQuantity)
	{
		public int MissingQuantity => unchecked(RequestedQuantity - AvailableQuantity);
	}

	/// <summary>編成の数量不足。表示文はUI側で生成する。</summary>
	public sealed class StockShortageException : InvalidOperationException
	{
		public StockShortageException(StockShortageFailure failure)
			: base("Composition stock is insufficient.")
		{
			Failure = failure;
		}

		public StockShortageFailure Failure { get; }
	}
}
