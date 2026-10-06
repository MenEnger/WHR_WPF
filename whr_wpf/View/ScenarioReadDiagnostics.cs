using System;
using System.IO;
using System.Security;
using System.Text;
using whr_wpf.Util;

namespace whr_wpf.View
{
    /// <summary>シナリオ読込の詳細を、UIホストの診断ファイルへ保存する。</summary>
    public static class ScenarioReadDiagnostics
    {
        public static string TryWrite(Exception error, string filePath = null)
        {
            ArgumentNullException.ThrowIfNull(error);
            try
            {
                if (filePath == null)
                {
                    var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    if (string.IsNullOrEmpty(directory)) return null;
                    filePath = Path.Combine(directory, "WHR_WPF", "Logs", "scenario-read.log");
                }
                var absolutePath = Path.GetFullPath(filePath);
                var record = new StringBuilder().AppendLine($"[{DateTimeOffset.UtcNow:O}]");
                if (error is ScenarioReadException readError)
                {
                    record.AppendLine($"Reason: {readError.Error}");
                    record.AppendLine($"Stage: {readError.Stage}");
                    record.AppendLine($"File: {readError.FilePath}");
                    record.AppendLine($"Property: {readError.PropertyName}");
                    record.AppendLine($"Line: {readError.LineNumber}");
                    record.AppendLine($"Mode: {readError.ModeNumber}");
                }
                record.AppendLine(error.ToString()).AppendLine();
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.AppendAllText(absolutePath, record.ToString(), new UTF8Encoding(false));
                return absolutePath;
            }
            // 診断の保存失敗で、元の読込失敗やメニューの案内を置き換えない。
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (SecurityException) { return null; }
        }
    }
}
