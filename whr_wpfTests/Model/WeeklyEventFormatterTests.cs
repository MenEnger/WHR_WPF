using whr_wpf.View;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class WeeklyEventFormatterTests
    {
        public static IEnumerable<object[]> CurrentMessages()
        {
            for (int i = 0; i < ExpectedMessages.Length; i++)
                yield return [WeeklyEventFixture.ExpectedEvents[i], ExpectedMessages[i]];
            yield return [new EngineDevelopedEvent(PowerEnum.Electricity, EngineDevelopmentKind.Available, 0, 60),
                "電気モーターが完成しました\n電車が作成できるようになります"];
            yield return [new EngineDevelopedEvent(PowerEnum.Diesel, EngineDevelopmentKind.Available, 0, 40),
                "ディーゼル機関が完成しました\nディーゼルカーが作成できるようになります"];
            yield return [new EngineDevelopedEvent(PowerEnum.LinearMotor, EngineDevelopmentKind.Available, 0, 300),
                "リニアが完成しました\nリニアカーが作成できるようになります"];
            yield return [new WarEndedEvent(1881, 1882, 120),
                "戦時体制は終了しました"];
        }

        [DataTestMethod]
        [DynamicData(nameof(CurrentMessages), DynamicDataSourceType.Method)]
        public void CurrentJapaneseMessagesAndLineBreaksArePreserved(GameEvent notification, string expected)
            => Assert.AreEqual(expected, WeeklyEventFormatter.Format(notification));

        [TestMethod]
        public void MultipleWeeksAreJoinedInOrderAndEmptyWeeksAddNoSeparator()
        {
            Assert.AreEqual("", WeeklyEventFormatter.FormatMany([]));
            var events = new List<GameEvent>();
            var game = WeeklyEventFixture.SimultaneousEvents();
            for (int i = 0; i < 4; i++) events.AddRange(game.NextWeek());
            Assert.AreEqual(string.Join("\n===\n", events.Select(WeeklyEventFormatter.Format)),
                WeeklyEventFormatter.FormatMany(events));
            // 先頭週の19件に、後続週の動力改良が続く。複数週をまとめても空週の区切りは付けない。
            CollectionAssert.AreEqual(ExpectedMessages, events.Take(19).Select(WeeklyEventFormatter.Format).ToArray());
            Assert.IsTrue(events.Count > 19);
        }

        private sealed record UnknownEvent : GameEvent;

        [TestMethod]
        public void UnknownEventAndDevelopmentKindsAreRejected()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => WeeklyEventFormatter.Format(new UnknownEvent()));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => WeeklyEventFormatter.Format(
                new EngineDevelopedEvent(PowerEnum.Electricity, (EngineDevelopmentKind)99, 0, 60)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => WeeklyEventFormatter.Format(
                new SpecialTechnologyDevelopedEvent((SpecialTechnology)99, 0, 0)));
        }

        internal static readonly string[] ExpectedMessages =
        [
            "蒸気機関の改良が完了しました\n蒸気機関車の開発可能速度が45km/hになります。",
            "電気モーターが改良されました\n電車の開発可能速度が360km/hになります",
            "電気モーターが改良されました\n電車の作成コストが下がります",
            "ディーゼル機関が改良されました\nディーゼルカーの開発可能速度が360km/hになります",
            "ディーゼル機関が改良されました\nディーゼルカーの作成コストが下がります",
            "リニアが改良されました\nリニアの開発可能速度が310km/hになります",
            "閉塞信号が完成しました\n運行可能数が増加します",
            "転換クロスシートが完成しました\n通勤列車にも使えるクロスシートです",
            "自動改札機が完成しました\n客一人当たりのコストが下がります",
            "振子式車体傾斜装置が完成しました\n対応車では、路線最高速度を20%上越えることができます",
            "豪華クロスシートが完成しました\n最高の乗り心地を保障する座席です",
            "収納式ロングシートが完成しました\n普通のロングシートよりも定員数が多くなります",
            "デュアルシートが完成しました\n乗り心地と定員数を両立させた座席です",
            "機械式車体傾斜装置が完成しました\n①簡易タイプでは、振子式と同性能で価格が安くなります。\n②高性能タイプは、路線最高速度を33%越えることができます。",
            "フリーゲージトレインが完成しました\nフリーゲージトレインは、狭軌・標準軌関係なく走ることができます",
            "移動閉塞信号が完成しました。\n運行可能数が増加します",
            "今年から、蒸気機関車の設定が不可能になります。\n（現在設定中のものは引き続き使用可能です）",
            "今年より戦時体制に突入します。\n貨物取扱量が変化し、貨物輸送を削減することができなくなります。",
            "おめでとうございます！\n目標を達成しました。フリーモードに移行しました。"
        ];
    }
}
