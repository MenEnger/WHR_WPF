# ADR 0021：選択肢の同一性を表示名から分離する

- 状態：採用
- 日付：2026-10-08
- 関連：Issue #27

## 背景

待避線・線数・路線規格の選択肢は型付きEqualsでCaptionを比較し、object.Equalsとhashは参照を使用していた。表示名を変更した同値の別インスタンスを渡すと、実際のWPF画面で選択が外れる、または以前の選択が残る。表示の変更が選択肢の意味を変えない境界が必要である。

## 判断

TaihiViewComponentは待避線enum、LaneNumViewModelは線数、RailTypeViewModelは軌道・nullable軌間・電化だけで比較する。object.Equalsは型付きEqualsへ委譲し、hashも同じ値から求める。Captionの変更・重複は同一性に影響しない。リニアを含め値を正規化しない。

## 代替案

- 型付きEqualsだけを修正する：object比較とhashの契約が食い違うため不採用。
- Captionを識別子として維持する：表示変更や重複が選択に影響するため不採用。
- 共通比較基底・不変選択肢・リストキャッシュへ再構成する：既存3型へ直接実装すれば足り、APIや資源の寿命を変える必要がないため不採用。

## 影響

公開APIの削除はないが、公開3型の比較意味は変わる。Captionだけが異なる同値要素が一致し、object比較やハッシュ集合も値を基準にする。値プロパティの可変性は維持するため、HashSet/Dictionaryのキーとして保持中は値を変更しない。現行製品にその利用は見つからない。Captionだけの変更ではhashも変わらない。

既存のリスト生成・表示名・XAML・建造や設備費用の計算は変更しない。通常/休止の初期選択と、二つの実バインド方式で別インスタンスの照合・UIからの書戻しを検証する。ItemsSourceに存在しない保存線数の復元候補はdocs/bug-notes.mdへ分離した。enum表示名とLine.Captionの分離は #27の後続段階。

仕様・影響、設計と検証記録はdocs/design/selection-value-equality.md。
