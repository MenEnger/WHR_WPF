using System;
using System.IO;
using System.Windows.Media.Imaging;
using whr_wpf.Util;
using whr_wpf.Model;

namespace whr_wpf.View
{
    public sealed class ScenarioPresentation
    {
        public GameInfo GameInfo { get; }
        public BitmapImage MapImage { get; }

        public ScenarioPresentation(GameInfo gameInfo)
            : this(gameInfo, string.IsNullOrEmpty(gameInfo.MapImagePath) ? null : LoadMap(gameInfo.MapImagePath))
        {
        }

        private ScenarioPresentation(GameInfo gameInfo, BitmapImage mapImage)
        {
            GameInfo = gameInfo;
            // 読込済み画像を難易度・モード選択からゲーム画面へ引き継ぐ。
            MapImage = mapImage;
        }

        public static ScenarioPresentation LoadScenario(string baseDirectory, Func<string, BitmapImage> readMap = null)
        {
            BitmapImage mapImage = null;
            // 旧読込順と同じく、設定検証後・CSV解析前に表示側で画像を確保する。
            var gameInfo = ScenerioLoadUtil.LoadFile(baseDirectory,
                path => mapImage = (readMap ?? LoadMap)(path));
            return new ScenarioPresentation(gameInfo, mapImage);
        }

        public static BitmapImage LoadMap(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var image = new BitmapImage();
            image.BeginInit();
            // 描画後もファイルを保持せず、読み込んだ画像を表示側で再利用する。
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static string FormatValidationError(ScenarioValidationException error) => error.Error switch
        {
            ScenarioValidationError.InvalidVersion => "シナリオバージョンの数値が異常です",
            ScenarioValidationError.InvalidBasicYear => "基礎とする年の値が異常です",
            _ => throw new ArgumentOutOfRangeException(nameof(error))
        };

        public static string FormatReadError(ScenarioReadException error)
        {
            var reason = error.Error switch
            {
                ScenarioReadError.FileNotFound => "必要なファイルが見つかりません。",
                ScenarioReadError.AccessDenied => "ファイルを読み取る権限がありません。",
                ScenarioReadError.IoFailure => "ファイルを読み取れませんでした。",
                ScenarioReadError.MissingValue => "必須の設定項目がありません。",
                ScenarioReadError.InvalidNumber => "数値の書式が正しくありません。",
                ScenarioReadError.NumberOutOfRange => "数値が読み取れる範囲を超えています。",
                ScenarioReadError.InvalidFormat => "データの書式が正しくありません。",
                ScenarioReadError.MissingField => "必要な項目が足りません。",
                ScenarioReadError.InvalidReference => "参照先の番号が正しくありません。",
                ScenarioReadError.InvalidSetting => "設定の組み合わせが正しくありません。",
                _ => throw new ArgumentOutOfRangeException(nameof(error))
            };
            var stage = error.Stage switch
            {
                ScenarioReadStage.Settings => "設定",
                ScenarioReadStage.Stations => "駅",
                ScenarioReadStage.Lines => "路線",
                ScenarioReadStage.Longways => "乗り継ぎ",
                ScenarioReadStage.Diagrams => "運転系統",
                ScenarioReadStage.Modes => "モード",
                _ => throw new ArgumentOutOfRangeException(nameof(error))
            };
            // 内部例外の文言を案内に混ぜず、入力を直すための出典だけを表示する。
            var source = $"解析対象: {stage}";
            if (!string.IsNullOrEmpty(error.FilePath)) source += $"\nファイル: {error.FilePath}";
            if (error.LineNumber.HasValue) source += $"\n行: {error.LineNumber.Value}";
            if (error.ModeNumber.HasValue) source += $"\nモード: {error.ModeNumber.Value}";
            if (!string.IsNullOrEmpty(error.PropertyName)) source += $"\n項目: {error.PropertyName}";
            return reason + "\n" + source;
        }

        public static string FormatUnexpectedReadError()
            => "シナリオを読み込めませんでした。ファイルと地図画像を確認してください。";
    }
}
