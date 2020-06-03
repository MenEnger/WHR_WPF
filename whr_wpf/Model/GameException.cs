using System;

namespace whr_wpf.Model
{

	/// <summary>
	/// 資金不足
	/// </summary>
	/// <param name="message"></param>
	public class MoneyShortException : InvalidOperationException
	{
		public MoneyShortException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// 編成が路線に不適合
	/// </summary>
	public class CompositionNotAppliedException : InvalidOperationException
	{
		public CompositionNotAppliedException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// データ不整合などで、これ以上処理続行できない例外
	/// </summary>
	public class CannotContinueException : InvalidOperationException
	{
		public CannotContinueException(string message) : base(message) { }
	}

}
