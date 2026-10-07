# 本体試験の追加移管：第二段階

2026-10-08。Issue #28。PR #50の本体抽出後。

## 仕様検討

製品の処理を変えず、WPF非依存で検証できる既存試験を増やす。Core-onlyのwhole-file試験10ファイルをwhr_coreTestsへ移す。WeeklyNotificationはUI試験が期待値を利用しているため後続へ残す。Scenario/mixed試験と共通操作窓口は別の段階で扱う。

## 影響確認

対象はCompositionCompatibilityTests、ConstructionRefusalTests、GoalDeadlineRegressionTests、LineEquipmentRefusalTests、ModeCurrentBehaviorTests、MoneyGoalFailureTests、NetworkCurrentBehaviorTests、ServiceSettingRefusalTests、StockShortageTests、VehicleValidationBehaviorTests。現状assert・データ・namespaceは維持する。

CurrentBehaviorFixtureはCoreだけを使う既存共通ソース。今回はwhr_coreTests/Modelへ配置し直し、残るWPF試験からCompile Linkする。本体試験がWPFフォルダを起点にしない配置にするが、fixture複製やテストassembly参照を作らない。シナリオfixture/イベント共有期待値は触らない。

検証は変更前の対象テスト名と件数を記録し、変更後Core/Windowsの合計533件と照合する。移動内容・各testNameの所属を比較して重複/脱落を確認する。製品変更がないので配布確認の反復や新しい挙動テストは追加しない。Refs #28/#23。

対象10ファイルは依存調査でCore/BCLとCurrentBehaviorFixtureだけを使用し、外部から共有helperを参照されていないことを確認した。135ケースを旧配置で実行し全成功。移動後予定はCore242件・Windows291件。WeeklyNotificationはExpectedEventsとSimultaneousEventsをUI試験が参照するため後段へ残す。

## 仕様・影響レビュー

Astra承認。Core-only依存、fixture配置、WeeklyNotificationを残す範囲と検証に修正必須指摘なし。

## 設計

対象10ファイルをwhr_wpfTests/Modelからwhr_coreTests/Modelへ物理移動する。CurrentBehaviorFixtureも同じ方向へ移し、内容・namespace・型の可視性・assertは編集しない。CoreTestsのCompile Include旧fixtureリンクを削除しSDKの標準Compileを使う。WPFTestsへ ../whr_coreTests/Model/CurrentBehaviorFixture.cs のCompile LinkをModel/CurrentBehaviorFixture.csとして追加する。

プロジェクト参照・対象framework・パッケージ版・コンパイラ設定・sln・製品ソースは変更しない。両assemblyがfixtureをそれぞれコンパイルするが、testmethodを持たないfixtureなのでテストは重複しない。新しい共有ライブラリやテスト間ProjectReferenceは追加しない。

移動前135件のTRXに加えPR #50のCore107/Windows426の全TRXを基準にし、移動後Core242/Windows291とテスト名集合を比較する。全533件成功・試験間重複0・全体の脱落0、135件の所属がCoreへ変わったことを確認する。移動11ファイルの内容一致を確認する。ソリューションtestは環境の並列MSBuild問題を避け-m:1、実行中のアプリ出力を避けartifactsへ出す。

fixtureの配置変更はADR 0024の単一ソース共有判断の継続とし、第一段階の旧配置記録を書き換えず後続の実施として追記する。Refs #28/#23。採用待ちの新仕様はない。

## 設計レビュー

Astraが正式設計を承認した。物理移動・リンク方向・試験集合の確認と変更規模に修正必須の指摘なし。

## 実装・検証

移動11ファイルのSHA256一致。csprojはfixtureリンクの削除・追加1行ずつのみ。全solution test（-m:1、BaseOutputPath=artifacts/core-test-separation/、UseSharedCompilation=false）でCore242件・Windows291件＝533件成功、スキップ0。

PR #50の全533テスト名と移動後の全テスト名集合が一致。Core/Windows間の名前重複0。baseline135件は全てCoreに所属。証拠はartifacts/core-test-separation-baseline/baseline.trxとartifacts/core-test-separation-results/*.trx。NuGet脆弱性情報の取得にNU1900警告あり。Linux実行は未実施。

## 仕上げレビュー

Astraが差分・試験網羅性・可読性・保守性・過剰さを承認。移動11ファイルの内容一致、リンク2行、全533件TRXと所属変更を独立確認し、修正必須指摘なし。
