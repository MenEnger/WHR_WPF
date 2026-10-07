# 週次通知試験と表示試験の共有データ分離

2026-10-08。Issue #28。PR #51の後続。

## 仕様検討

週次処理の構造化イベント・順序・途中状態・不変性を検証するWeeklyNotificationBehaviorTestsをWPF非依存のCoreTestsへ移す。表示文字列を検査するWeeklyEventFormatterTestsはWPFTestsに残す。製品挙動・試験入力・期待値・ケースは変えない。

## 影響確認

表示試験は通知試験クラスのExpectedEventsとSimultaneousEventsを参照する。この2つはモデルだけを使う共通データ/生成処理なので、CoreTests内のWeeklyEventFixtureを正本として抽出し、WPFTestsから単一ソースをCompile Linkする。CurrentBehaviorFixtureは両試験で使用済みのソースリンクを維持する。試験クラスをassembly間参照する構成や共有パッケージは追加しない。

構造化イベントの期待配列と画面用ExpectedMessagesの対応順を維持する。DynamicDataの公開CurrentMessagesメソッド、テスト名・引数・DataRow・期待assertは維持する。失敗週のイベント未返却・変更済み状態の保持はADR 0019の契約であり移管で変えない。解析・他混在試験・共通操作窓口は後段へ残す。Refs #28/#23。

対象2試験を変更前に実行し、移動後全533件の名前/ケース数・所属・重複・脱落を確認する。製品無変更につき追加の挙動テスト・配布再検証は不要。

全参照調査：ExpectedEventsは通知2箇所・Formatter1箇所、SimultaneousEventsは通知3箇所・Formatter1箇所。ほかの呼出なし。通知15件・表示25件のbaseline40件は全成功。移動後予定はCore257件・Windows276件＝533件。

## 仕様・影響レビュー

Astra承認。イベント期待値と生成処理だけを共有し、表示文・DynamicDataをWPFに残す範囲に修正必須指摘なし。

## 設計

CoreTests/Model/WeeklyEventFixture.csにinternal static class WeeklyEventFixtureを置く。namespaceはwhr_wpf.Model.Tests。既存ExpectedEvents宣言（readonly配列19件）とSimultaneousEventsメソッドを内容・可視性・順序を変えず移す。CurrentBehaviorFixtureのstatic usingを付ける。試験属性を付けない。

WeeklyNotificationBehaviorTestsをCoreTests/Modelへ移し、WeeklyEventFixtureのstatic usingを追加する。抽出した2member以外はtestmethod・assert・DataRowを変更しない。FormatterはWeeklyNotificationBehaviorTests.参照2箇所をWeeklyEventFixture.へ置換する。WPFTestsに新fixtureのCompile Linkを1行追加する。CoreのSDK既定Compileに新fixtureが入り、ProjectReferenceやfriendは増やさない。

baseline40件とPR #51全533件のTRXを照合する。全533件成功、Core257・WPF276、case名/引数が全体で一致、重複0、通知15件だけ所属がCoreへ変更されたことを確認する。製品変更なし。-m:1と独立BaseOutputPathでsolution testを実行する。

採用判断はADR 0024の単一fixture共有と段階移管の継続として追記する。readonly配列をimmutable APIへ変更するなど別目的の改修は行わない。共有memberの所有者を移すことでUIが本体試験クラスを必要としない配置にする。

## 設計レビュー

Astra承認。抽出2要素・通知移管・表示参照2箇所・リンク1行、ケース照合計画に修正必須指摘なし。

## 実装・検証

全solution test（-m:1、BaseOutputPath=artifacts/weekly-test-separation/、UseSharedCompilation=false）でCore257件＋WPF276件＝533件成功、スキップ0。PR #51全533件とのtestName（DataRow/DynamicDataの引数を含む）集合一致、重複0。baseline40件のうち通知15件すべてCore側へ移管、表示25件はWindows側。

共有2memberの原文と抽出前が一致し、通知試験は2memberの抽出とstatic using以外の本体内容が一致。Formatter差分は参照2箇所、csprojリンク1行。製品変更なし。証拠はartifacts/weekly-test-separation-baseline/baseline.trx、artifacts/weekly-test-separation-results/*.trx。NuGet脆弱性情報取得にNU1900警告。Linux実行未実施。

## 仕上げレビュー

Astraが差分・網羅性・可読性・保守性・過剰さを承認。抽出原文・assert維持、Formatter2参照・リンク1行、全533件のTRXとcase名/引数一致を独立確認。修正必須指摘なし。
