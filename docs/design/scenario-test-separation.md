# シナリオ試験のWPF非依存構成

2026-10-08。Issue #28。PR #52の後続。

## 仕様検討

旧形式シナリオの読込・設定検証・エラー優先順・callback順とホスト標準出力への非混入を、WPFなしで既存試験から確認する。net9.0のwhr_scenarioTestsを追加し、ScenarioBoundaryTests・ScenarioFailureBehaviorTests・DiagnosticBoundaryTestsを移管する。製品・解析API・入力・期待値・assertは変えない。

## 影響確認

ScenarioBoundaryTests内のScenarioFilesは、読込試験とWPFの画像・診断・ログ試験が共有する。net9.0試験側の同名namespaceにinternal sealedな独立ScenarioFilesとして置き、WPFから単一ソースをCompile Linkする。元のファイル生成、UTF8/CP932、BMP、削除/置換/Disposeの処理を維持する。nested型から独立型へ移るため、旧試験クラスのstatic usingを全呼出元で除く。製品のpublic APIは変更しない。

Scenario試験はCoreとScenarioだけを参照し、CurrentBehaviorFixtureはCoreTestsから単一ソースをリンクする。試験assembly同士のProjectReferenceやfriendは追加しない。DiagnosticBoundaryのDoNotParallelizeとConsole復元を維持する。共有ScenarioFilesの読込資源寿命やエラー優先順は移管で変えない。

混在ScenarioReadDiagnosticsTestsの解析assertと表示assertの切分けは後段に残す。OriginalStatePort/SelectedBehavior等も後段。今回は解析単独利用の既存成功・失敗テストを実際のnet9.0 assemblyで実行する範囲であり新たな製品契約を追加しない。Refs #28/#23。

対象3試験を旧配置で実行しbaselineを記録する。移管後の全533件と名前/引数/所属・重複/脱落を照合し、ScenarioTestsのFrameworkReference/ProjectReferenceにWPF依存がないことを確認する。製品変更がないため配布検証と追加の挙動テストは不要。

全呼出調査：ScenarioFiles利用はBoundaryと5外部ファイル(Failure/Diagnostic/ReadDiagnostics/ReadLog/Presentation)だけ。完全修飾nested参照なし。Boundary10・Failure7・Diagnostic2＝19件は旧配置で全成功。移管後予定はCore257＋Scenario19＋WPF257＝533件。ReadDiagnostics76件は解析65/表示11の混在のため後段へ残す。

## 仕様・影響レビュー

Astra承認。解析単独の試験プロジェクト、共有資源fixture、Console復元/順序契約と対象範囲に修正必須指摘なし。

## 設計

whr_scenarioTests/whr_scenarioTests.csprojはnet9.0、MSTest3.6.4/TestSdk17.12.0、Nullable/ImplicitUsings enable、LangVersion latest、MSTest Usingを既存に合わせる。CoreとScenarioへのProjectReferenceだけを置き、CurrentBehaviorFixtureをCoreTestsからModel/CurrentBehaviorFixture.csとしてCompile Linkする。slnへAnyCPU Debug/Releaseで登録する。WPFの設定・製品参照は変えない。

ScenarioBoundaryTests、ScenarioFailureBehaviorTests、DiagnosticBoundaryTestsをModelへ物理移動する。Boundary内のinternal sealed ScenarioFilesを同namespaceのModel/ScenarioFiles.csへ抽出する。入れ子を外すインデントだけを変え、raw stringの生成値を含め処理・可視性・資源確保/破棄順は維持する。Boundaryでは不要となるSystem.Text usingを除く。5呼出ファイルの旧Boundary static usingを削除し、同namespaceの型で解決する。WPFTestsへ新fixtureのCompile Link1行だけ追加する。

試験名、DataRow、assert、TestCleanup、DoNotParallelize、Console復元を変えない。fixtureは試験属性なし。assembly間の試験参照やfriendを追加しない。テスト用型のnested identity変更は製品public APIの互換性変更ではない。

移管前baseline19とPR #52全533件TRXのtestName/引数集合を比較し、Core257・Scenario19・WPF257＝533件成功、3assembly間重複0、19件の所属変更を確認する。FrameworkReferenceは.NETCoreのみ、ProjectReferenceはCore/Scenarioだけであることを確認する。独立ScenarioTestsのnet9.0実行と、solution test -m:1（独立BaseOutputPath、UseSharedCompilation=false）でWPFの資源寿命・診断試験も確認する。個別実行とsolution全体を必要なく二重反復せず、solution中のScenarioTests出力と依存情報を証拠にする。Linux実行済みとは主張しない。

判断はADR 0024のプロジェクト境界とfixture単一ソース共有の継続として追記する。混在76件の分割、操作窓口、既知不具合は追加しない。

## 設計レビュー

Astra承認。プロジェクト設定・移動/共有・raw string出力・資源寿命・全件照合と依存確認に修正必須指摘なし。

## 実装・検証

全solution test（-m:1、BaseOutputPath=artifacts/scenario-test-separation/、UseSharedCompilation=false）でCore257＋Scenario19＋WPF257＝533件成功、スキップ0。PR #52の全class名＋case名/引数と一致、重複0。baseline19件の全てがScenarioTestsへ所属変更。同名メソッドが異なる試験クラスにあるため、脱落/重複確認は完全クラス名も含めて比較した。

ScenarioTestsはnet9.0、FrameworkReferenceはMicrosoft.NETCore.Appのみ、ProjectReferenceはCore/Scenarioのみ。解析単独の試験assemblyで実行を確認した。証拠はartifacts/scenario-test-separation-baseline/baseline.trx、artifacts/scenario-test-separation-results/*.trx、artifacts/scenario-test-references.json。

fixtureはインデントを戻すと元本文と完全一致し、raw stringの本文と閉じdelimiterも同じ幅だけ変更したため生成値を維持。既存の読込データ/画像/診断/ログ試験も成功した。製品変更なし。NuGet脆弱性情報取得にNU1900警告。Linux実行未実施。

## 仕上げレビュー

Astraが差分・網羅性・可読性・保守性・過剰さを承認。fixture原文/raw string/寿命、assert/後処理、参照方向、全533件TRXと完全ケース名の差分0・重複0を独立確認。修正必須指摘なし。
