namespace whr_wpf.Model
{
	/// <summary>週次処理で発生した事実を、表示文言と可変モデル参照なしで保持する。</summary>
	public abstract record GameEvent;

	public enum EngineDevelopmentKind { Available, SpeedImproved, CostReduced }

	public enum SpecialTechnology
	{
		BlockingSignal, ConvertibleCross, AutoGate, CarTiltPendulum, RichCross,
		RetructableLong, DualSeat, MachineTilt, FreeGauge, DynamicSignal
	}

	public sealed record EngineDevelopedEvent(PowerEnum Power, EngineDevelopmentKind Kind,
		int PreviousLevel, int CurrentLevel) : GameEvent;
	public sealed record SpecialTechnologyDevelopedEvent(SpecialTechnology Technology,
		int PreviousCapacity, int CurrentCapacity) : GameEvent;
	public sealed record SteamAvailabilityEndedEvent(int Year) : GameEvent;
	public sealed record WarStartedEvent(int StartYear, int EndYear, int FreightIndex) : GameEvent;
	public sealed record WarEndedEvent(int StartYear, int EndYear, int FreightIndex) : GameEvent;
	public sealed record GoalsAchievedEvent(int PreviousDeadline, int FreeModeDeadline) : GameEvent;
}
