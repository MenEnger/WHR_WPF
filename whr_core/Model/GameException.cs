using System;

namespace whr_wpf.Model
{

	/// <summary>
	/// 資金不足
	/// </summary>
	public class MoneyShortException : InvalidOperationException
	{
		/// <param name="failure">判定時点の資金不足の情報。</param>
		public MoneyShortException(MoneyShortageFailure failure)
			: base("Available money is insufficient.")
		{
			Failure = failure;
		}

		public MoneyShortageFailure Failure { get; }
	}

	/// <summary>
	/// 編成が路線に不適合
	/// </summary>
	public class CompositionNotAppliedException : InvalidOperationException
	{
		public CompositionNotAppliedException(CompositionCompatibilityFailure failure)
			: base("Composition is not compatible with the line.")
		{
			Failure = failure;
		}

		public CompositionCompatibilityFailure Failure { get; }
	}

	/// <summary>
	/// データ不整合などで、これ以上処理続行できない例外
	/// </summary>
	public class CannotContinueException : InvalidOperationException
	{
		public CannotContinueException(string message) : base(message) { }
	}

	/// <summary>
	/// ゲームオーバー時の例外
	/// </summary>
	public class GameOverException : Exception
	{
		public GameOverException(GameOverFailure failure)
			: base("The game deadline was exceeded.")
		{
			Failure = failure;
		}

		public GameOverFailure Failure { get; }
	}

}
