using System;

namespace whr_wpf.Model
{

	/// <summary>
	/// 資金不足
	/// </summary>
	/// <param name="message"></param>
	class MoneyShortException : InvalidOperationException
	{
		public MoneyShortException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// 編成が路線に不適合
	/// </summary>
	class CompositionNotAppliedException : InvalidOperationException
	{
		public CompositionNotAppliedException(string message) : base(message)
		{
		}
	}

}
