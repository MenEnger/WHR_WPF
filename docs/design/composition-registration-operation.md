# 編成作成・登録の本体操作

2026-10-08。Refs #28 / #23。仕様・影響は[全操作調査](common-operation-audit.md)でAstraレビュー済み。

## 仕様・影響

編成作成はFactoryで検査/生成し、ゲームへの登録だけCompositionMakeViewModel.MakeCommand.Makeが直接行う。本体に作成・登録操作を追加し、UIはその操作を呼ぶ。正常結果と既存拒否/内部例外、資金消費なし、返値・登録の同一性を保持する。公開可変リストやFactory APIは今回削除しない。

新規負数量拒否、16両上限、車両所属、未知enum、入力snapshot、表示見積変更は対象外。#28の全入力検証完了を意味しない。採用済みのFactory検査は本体操作でも毎回行い、UIのCanExecuteだけには依存しない。

## 設計

GameInfoLogic.csへpublic Composition CreateComposition(string name, IEnumerable<KeyValuePair<Car,int>> vehicleNumbers)を追加する。登録先compositionsをlocalに取得→既存CompositionFactory.CreateComposition→取得済み登録先.Add→生成した同一Compositionを返す。登録先の評価順は旧VMのcompositions.Add(Factory...)と同じ。Factoryの入力列挙回数、名前優先検査、未知例外は保持し、登録失敗の原子性や巻戻し/一括拒否変換を追加しない。

VMのMakeCommand.MakeはgameInfo.CreateComposition(Name, VehicleNums)を呼ぶ。確認/取消/CanExecute/Description/ErrorMsgはそのまま。生成結果は登録されるだけで購入されず、HeldUnits・Moneyは変えない。新イベント/結果型/共通facade/プロジェクト/Factoryの公開互換変更は不要。

検証はCoreで(1)正常作成の同一返値・一度だけ登録・入力車両と数量/0数量除外・資金/余剰保持、(2)既存の構造化拒否で登録と資金保持、(3)列挙例外の同一伝播と登録未到達、(4)列挙中に公開登録先fieldが置換されても旧式と同じ先へ登録、を確認する。Factoryの各検査は既存VehicleValidationBehaviorTestsへ委ねて重複しない。

WPFでは実VMの選択・数量・名前を設定し、MakeCommandの確認後コールバックMakeをreflectionで直接呼び、設定入力に対応した編成が一度登録されることを確認する。MessageBox自動操作や表示文言固定は追加しない。既存Factory全ケースと全solution試験を独立出力で実行する。

## 設計レビュー

Astra承認。登録先の先行評価、Factory再列挙/内部例外維持、同一結果一度登録、Core4観点とUI確認後callback検査に修正必須指摘なし。UI検査が確認/取消の実ダイアログ試験ではないことを検証結果へ明記する。


## 実装・検証

設計どおり本体操作とVMコールバックを接続した。変更前Factory判定36件成功。独立BaseOutputPath、UseSharedCompilation=false、solution test -m:1で全538件成功（Core294/Scenario84/WPF160、失敗・スキップ0）。追加5件は登録境界・確認後の接続を対象とし、Factoryの検査36件を重複しない。

TRXはartifacts/composition-registration-results、事前試験はartifacts/composition-registration-baseline-results。NuGet脆弱性情報取得NU1900警告は継続しているが、復元・ビルド・テストは成功。実ダイアログの確認/取消操作は未検証。今回はプロジェクト/配布構成を変更していない。

## コードレビュー

Astra承認。登録先評価順、Factory判定/再列挙/例外保持、同一編成一度登録、VM接続、追加5件の網羅性、可読性・保守性・過剰さに修正必須指摘なし。全538件のTRXも確認済み。

