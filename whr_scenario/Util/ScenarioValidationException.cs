using System;

namespace whr_wpf.Util
{
    public enum ScenarioValidationError
    {
        InvalidVersion,
        InvalidBasicYear
    }

    /// <summary>表示文言と終了判断を含まないシナリオ値の検証エラー。</summary>
    public sealed class ScenarioValidationException : Exception
    {
        public ScenarioValidationError Error { get; }
        public string FilePath { get; }
        public string PropertyName { get; }
        public int Value { get; }

        public ScenarioValidationException(ScenarioValidationError error, string filePath, string propertyName, int value)
            : base($"{error}: {propertyName}={value} ({filePath})")
        {
            Error = error;
            FilePath = System.IO.Path.GetFullPath(filePath);
            PropertyName = propertyName;
            Value = value;
        }
    }
}
