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
    }
}
