# シナリオ診断試験の解析・表示分離

2026-10-08。Issue #28、PR #53の後続。

## 仕様検討

ScenarioReadDiagnosticsTestsの解析65件をScenarioTestsへ移し、UIの案内文を検証する11件をWPFTestsへ残す。製品コード・解析順序・例外情報・入力/期待値・assertを変えず、WPFを参照しない解析試験の範囲を広げる。

## 影響確認

表示専用2メソッドUiMessagesUseTheReasonAndSourceWithoutExposingInternalMessages（10 DataRow）とUiReasonsHaveDifferentGuidanceAndUnexpectedFailureHasGenericGuidance（1件）を、責務名のScenarioReadFailureFormatterTestsへ分ける。解析クラスは元名を維持し、モデル/解析とScenarioFilesだけを利用する。ReadFailure/AssertSourceのprivate helperは解析側に残す。表示専用メソッドはシナリオファイルの生成やこのhelperを必要としない。

ケース名と引数は維持するが、表示11件の完全クラス名は意図して変更する。検証ではこの11件だけ旧→新クラスへ対応付け、ほかの完全ケース名をそのまま比較する。別assemblyで同じ公開試験クラス名を併存させず、責務と配置を名前で追えるようにする。

移管前76件を実行し、全533件（予定Core257/Scenario84/WPF192）の成功・重複/脱落・所属を確認する。プロジェクト設定・fixture・製品は変更しない。OriginalStatePort/SelectedBehavior等と共通操作窓口は後段へ残す。Refs #28/#23。

全呼出調査：解析21メソッド65件、表示2メソッド11件。ReadFailure/AssertSourceは解析内専用で外部参照なし。解析にMalformedLineException用Microsoft.VisualBasic.FileIO usingを維持する。表示側にScenarioFiles/helper依存はない。変更前76件は全成功。

## 仕様・影響レビュー

Astra承認。解析/表示の責務境界、private helper維持、新fixture不要、限定したclass名対応に修正必須指摘なし。

## 設計

元ファイルをwhr_scenarioTests/Model/ScenarioReadDiagnosticsTests.csへ物理移動し、表示専用2メソッド（属性/本文全て）を取り出し、不要なusing whr_wpf.Viewだけを除く。解析の21メソッドとReadFailure/AssertSource、namespaceとclass名は変更しない。

取り出した2メソッドをwhr_wpfTests/View/ScenarioReadFailureFormatterTests.csへ配置する。namespaceは比較差分を小さくするため既存whr_wpf.Model.Testsを維持、class名はScenarioReadFailureFormatterTestsとする。usingはwhr_wpf.Util/whr_wpf.Viewを使い、ImplicitUsingsを利用する。製品・csproj・sln・fixtureの編集なし。

class対応は2メソッドに限定する：UiMessagesUseTheReasonAndSourceWithoutExposingInternalMessagesの10DataRowとUiReasonsHaveDifferentGuidanceAndUnexpectedFailureHasGenericGuidanceの1件、旧whr_wpf.Model.Tests.ScenarioReadDiagnosticsTests→新whr_wpf.Model.Tests.ScenarioReadFailureFormatterTests。メソッド名と引数は同じ。

baseline76と前段全533のTRXを比較する。完全クラス名＋case名/引数でキーを作り、旧11件だけ新classへ対応付ける。元76ケースと分割後65/11ケースの対応・全533集合一致・重複0を確認する。Core257/Scenario84/WPF192の成功と、解析65のScenario所属・表示11のWPF所属を検証する。各移動メソッド/属性/private helperの本文一致も確認する。solution test -m:1、独立BaseOutputPath、UseSharedCompilation=falseで実行する。

既存のプロジェクト境界を利用する分割であり、ADR 0024の継続として記録する。後続のモデル/UI混在と操作窓口は追加しない。製品変更がないため配布再検証や新規挙動テストを増やさない。

## 設計レビュー

Astra承認。移動対象・属性/本文維持・表示11件だけのclass対応と所属検証、既存構成の利用に修正必須指摘なし。

## 実装・検証

全solution test（-m:1、BaseOutputPath=artifacts/scenario-diagnostic-split/、UseSharedCompilation=false）でCore257＋Scenario84＋WPF192＝533件成功、スキップ0。旧表示11件のclassだけ対応付け、前段全533の完全クラス名＋case名/引数と一致、重複0。baseline76件の解析65/表示11への対応と所属を確認した。

UI attributes/DataRow/body原文一致。解析にUI blockを戻すと不要なView using除去以外は元本文と一致し、private helperも維持。製品・設定・fixture変更なし。証拠はartifacts/scenario-diagnostic-split-baseline/baseline.trxとartifacts/scenario-diagnostic-split-results/*.trx。NuGet脆弱性情報取得にNU1900警告。Linux実行未実施。

## 仕上げレビュー

Astraが差分・網羅性・可読性・保守性・過剰さを承認。表示属性/本文と解析内容維持、全533件TRXと限定class対応後のcase名/引数差分0・重複0を独立確認。修正必須指摘なし。
