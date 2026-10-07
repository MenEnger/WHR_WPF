# ADR 0015：編成売却・使用の数量不足を表示から分離する

- 状態：採用
- 日付：2026-10-07
- 関連：Issue #26、#28

## 背景

編成売却と2種類の編成の使用拒否は、日本語InvalidOperationException.Messageへ操作・不足数を埋め込む。UIや操作窓口が理由と数量を文章に依存せず受け取れるようにする。

## 判断

数量不足3箇所だけを専用のInvalidOperationException派生型へ移す。Use/Sale種別と要求数・判定時点の余剰数を不変recordへ保存し、不足数は現行と同じunchecked int減算で導出する。可変編成参照・名前・価格を収集しない。

default SaleはgameInfo null検査後に読んだHeldUnitsを判定と保存で共有する。価格計算→Use再検査→入金の順と、Useの負数検査を維持する。Sale経由でUse段階に不足する場合はUse拒否としてそのまま伝える。外部IComposition実装の例外を包み直さない。

Viewのformatterが既存2文章を生成し、ViewModelBaseが一般InvalidOperationExceptionより前に専用型を捕捉する。VMの確認・CanExecuteや割当の途中状態は変更しない。

## 代替案

- 日本語Messageを残して理由だけ追加：本体の表示責務が残るため不採用。
- 在庫操作を共通サービスへ移す：3拒否の情報分離より範囲が広く、今回必要がないため不採用。
- 不足数をlongへ変更：不正な負在庫で現行表示が変わるため今回は不採用。数値境界の補正は別判断。
- Line/Keitoの不足判定や旧割当解放順も修正：既知の数式・途中状態の仕様判断に踏み込むため今回は不採用。

## 影響

操作シグネチャと基底InvalidOperationExceptionでの捕捉互換は維持する。厳密な例外型比較と日本語Message依存は移行が必要。Line/Keitoの事前不足は一般例外、解除後のUse不足は専用型となるが、発生条件と途中状態は維持する。

責務分離と可読性・保守性のため少数の型と3拒否箇所に限定し、既存成功・割当途中状態テストを再利用する。仕様・影響・設計・検証はdocs/design/stock-shortages.md。不具合候補はdocs/bug-notes.md、主要導線E2EはIssue #37。
