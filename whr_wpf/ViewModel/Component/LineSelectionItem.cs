using whr_wpf.Model;

namespace whr_wpf.ViewModel.Component
{
    /// <summary>
    /// 路線一覧の表示・文字入力検索と、選択したモデルへの参照を持つ。
    /// </summary>
    public class LineSelectionItem
    {
        public LineSelectionItem(Line line)
        {
            Line = line;
        }

        public Line Line { get; }

        public string Caption => $"{Line.Name} {Line.Start.Name}～{Line.End.Name}";
    }
}
