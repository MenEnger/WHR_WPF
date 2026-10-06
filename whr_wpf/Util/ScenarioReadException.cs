using System;

namespace whr_wpf.Util
{
	public enum ScenarioReadError
	{
		FileNotFound, AccessDenied, IoFailure, MissingValue, InvalidNumber,
		NumberOutOfRange, InvalidFormat, MissingField, InvalidReference, InvalidSetting
	}

	public enum ScenarioReadStage { Settings, Stations, Lines, Longways, Diagrams, Modes }

	/// <summary>表示文言を含めず、入力の失敗理由と出典を呼び出し側へ渡す。</summary>
	public sealed class ScenarioReadException : Exception
	{
		public ScenarioReadError Error { get; }
		public ScenarioReadStage Stage { get; }
		public string FilePath { get; }
		public string PropertyName { get; }
		public long? LineNumber { get; }
		public int? ModeNumber { get; }

		public ScenarioReadException(ScenarioReadError error, ScenarioReadStage stage,
			string filePath, Exception innerException = null, long? lineNumber = null,
			string propertyName = null, int? modeNumber = null)
			: base($"{stage}: {error}", innerException)
		{
			Error = error;
			Stage = stage;
			FilePath = filePath;
			LineNumber = lineNumber;
			PropertyName = propertyName;
			ModeNumber = modeNumber;
		}
	}
}
