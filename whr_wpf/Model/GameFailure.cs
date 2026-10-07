namespace whr_wpf.Model
{
	/// <summary>資金不足の判定時点の要求額と所持金。</summary>
	public sealed record MoneyShortageFailure(long RequestedAmount, long AvailableMoney);

	public enum GameOverReason
	{
		DeadlineExceeded,
	}

	/// <summary>ゲーム終了の理由と判定時点の年・実行中期限。</summary>
	public sealed record GameOverFailure(GameOverReason Reason, int Year, int DeadlineYear);
}
