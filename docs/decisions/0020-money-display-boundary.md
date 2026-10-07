# ADR 0020：金額表示を計算Utilから分離する

- 状態：採用
- 日付：2026-10-08
- 関連：Issue #27、#24、#28

## 背景

LogicUtilにゲームの計算と日本語金額の文章生成が同居している。金額表示を変更する際に計算へ影響しない境界を作る。

## 判断

AppendMoneyUnitの本体と私有ConvJapaneseNumeralをView/MoneyDisplayFormatterへ移し、Format(long amount)から呼ぶ。VMの全34利用箇所を更新し、表示される文章、10万円単位、Math.Absによる符号欠落、unchecked乗算と桁切捨て、乗算後long.MinValueでの例外を維持する。

金額計算・プロパティ名・確認と実行の順序は変更しない。確認文章の単位重複も混ぜない。負値表示と技術投資累計の意味は #24で別に扱う。

## 代替案

- LogicUtilに互換wrapperを残す：計算側から表示側への参照が残るため不採用。
- 符号やoverflowを同時に改善する：現状固定の移動へ仕様変更が混ざるため今回は不採用。
- 汎用数値整形サービス/インターフェースを作る：現在は一つの既存変換の移動だけで足りるため不採用。

## 影響

LogicUtil型はinternalで、通常外部assembly向け公開型APIではない。ただし旧publicメソッドを同一assembly・友達assembly・reflectionで呼ぶ利用側は移行が必要。旧メソッドを削除し、既存の待避線試験も新formatterへ直接接続する。

私有変換は外部互換を持たない。Modelや計算からの利用はなかったため、表示側への依存を本体へ追加しない。enum表示名・Line.Caption・選択肢Equalsは #27の後続段階。

検証はメソッド入口の固定期待値と既存VM/WPF表示回帰試験を使用し、同じ変換を全34箇所で重複検査しない。仕様・影響・設計・結果はdocs/design/money-display-boundary.md。
