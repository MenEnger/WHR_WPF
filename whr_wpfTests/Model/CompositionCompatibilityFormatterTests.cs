using System.Windows.Input;
using whr_wpf.View;
using static whr_wpf.Model.Tests.CurrentBehaviorFixture;

namespace whr_wpf.Model.Tests
{
    [TestClass]
    public class CompositionCompatibilityFormatterTests
    {
        [DataTestMethod]
        [DataRow(CompositionCompatibilityReason.RequiresLinearVehicle, "車両がリニアではありません")]
        [DataRow(CompositionCompatibilityReason.RequiresLinearTrack, "リニア軌道ではありません")]
        [DataRow(CompositionCompatibilityReason.GaugeMismatch, "路線幅が違います")]
        [DataRow(CompositionCompatibilityReason.RequiresElectrification, "路線が非電化です")]
        [DataRow(CompositionCompatibilityReason.SteamExpired, "蒸気機関車は時代遅れで使えません")]
        public void MessagesKeepExistingText(CompositionCompatibilityReason reason, string expected)
            => Assert.AreEqual(expected, CompositionCompatibilityFormatter.Format(new(reason, "試験線")));

        [TestMethod]
        public void UnknownReasonIsNotSilentlyAccepted()
            => Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                CompositionCompatibilityFormatter.Format(new((CompositionCompatibilityReason)999, "試験線")));

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DiagramViewModelConnectsSelectionMessageAndCommand(bool throughService)
        {
            var game = Game();
            var line = Line(game);
            var through = Through(game, line);
            var stock = Stock(game);
            string typeName = throughService ? "KeitoDiagramSettingViewModel" : "LineDiagramSettingViewModel";
            object target = throughService ? through : line;
            var type = typeof(GameInfo).Assembly.GetType($"whr_wpf.ViewModel.{typeName}")!;
            // 内部VMを公開せず、画面が使う公開プロパティとコマンドの接続を検査する。
            var vm = Activator.CreateInstance(type, game, target, null)!;
            string Message() => (string)type.GetProperty("ErrorMsg")!.GetValue(vm)!;
            bool CanExecute() => ((ICommand)type.GetProperty("Kettei")!.GetValue(vm)!).CanExecute(null);
            void Select(IComposition? composition) => type.GetProperty("Composition")!.SetValue(vm, composition);

            Assert.AreEqual("編成が未選択です", Message());
            Assert.IsFalse(CanExecute());
            type.GetProperty("RunningPerDay")!.SetValue(vm, 10);
            Select(stock);
            Assert.AreEqual("", Message());
            Assert.IsTrue(CanExecute());
            stock.Power = PowerEnum.Electricity;
            Select(stock);
            Assert.AreEqual("路線が非電化です", Message());
            Assert.IsFalse(CanExecute());
            stock.Power = PowerEnum.Steam;
            Select(stock);
            Assert.AreEqual("", Message());
            Assert.IsTrue(CanExecute());
            Select(null);
            Assert.AreEqual("編成が未選択です", Message());
            Assert.IsFalse(CanExecute());
        }
    }
}
