# ADR 0023：路線Captionを表示項目へ移す

- 状態：採用
- 日付：2026-10-08
- 関連：Issue #27、#23

## 背景

Line.Captionは路線名と始終点を結合し、一覧の表示と文字入力検索、シナリオ内部診断に使われる。表示を差し替える際にモデルの表示プロパティを変更しなくてよい境界が必要である。

## 判断

UIのLineSelectionItemに元Line参照とライブCaption getterだけを置く。現在の一覧を同順序の表示項目へ投影し、DisplayMemberPathで表示と検索を維持する。SelectedValuePath="Line"で元Lineを選び、初期選択とDropDownClosed後の画面遷移もSelectedValueの元Lineを使う。

モデルの公開Line.Captionは削除する。GameInfoの内部診断だけは同じ路線名・駅名をその箇所で組み立て、従来の診断文を維持する。診断の捕捉やモード失敗時の途中状態は変更しない。

## 代替案

- ItemTemplateのMultiBindingだけで表示する：WPFの文字入力検索は元項目の文字列を読むため既存検索を維持できず不採用。
- ModelのCaption/ToStringを残す：表示の責務を本体へ残すため不採用。
- 型記述子の登録や独自文字検索を追加する：単一の表示項目で足り、グローバルな仕組みや検索実装を管理する必要がないため不採用。

## 影響

公開Captionの削除はソース・バイナリ・reflection・外部XAMLバインドの互換性変更。製品内の2経路を移行する。LineListの項目はUIの表示項目になり、元LineはSelectedValueまたは項目のLineから取得する。モデル自体をコピーせず、ページで一覧の順序と元参照を維持する。動的な一覧・名称編集や通知機能は今回追加しない。

名称が空/nullの場合の区切りと日本語名、同名別区間、文字入力検索、遷移先が受け取る元Lineを検証する。駅名・車両名などのデータ文字列は本体に残す。内部診断は表示用APIではなく、従来の対象識別情報を保持する。仕様・影響・設計・完了照合はdocs/design/line-caption-boundary.md。
