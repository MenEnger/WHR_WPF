# 資金不足と期限終了を表示から分離する

2026-10-07。Issue #26の第七段階。

## 仕様検討

SpendMoneyの資金不足とCheckAchievementの期限超過の2発生箇所を構造化する。要求金額・判定時資金、期限超過の理由・判定年・実行中期限を不変データで保存し、日本語の案内2文章はViewへ移す。

支払条件・減算・負額や異常資金の現状、週次の処理順・期限優先・途中状態・通知返却を維持する。不足額を算出せず、フリーモード終了も現行の目標失敗文言を維持する。目標の有無や達成状態を新たに読み込んだり、失敗の理由を推測して区別したりしない。

## 影響確認

MoneyShortExceptionの生成はSpendMoneyだけ。表示先はViewModelBase、TaihisenChangeViewModel、ConstructWindowViewModelの3catchで現在固定文を表示する。GameOverExceptionの生成はCheckAchievementだけ、表示先はViewModelBaseとGamePageViewModelの週/月送り3catch。全catchをViewのformatterへ接続し、確認・閉じる・終了処理とtry範囲は維持する。

既存例外型と基底型は維持し、stringコンストラクタと日本語Message依存を終了する。リポジトリ内の全生成・catch・テストを検索済み。週送りはNextWeekがtry外にあるため終了例外が既存catchへ届かない。これは別の不具合で今回直さず、週次失敗契約の後続判断へ残す。

## 検証計画

変更前に支払不足・ちょうど足りる境界・成功/拒否の状態を固定する。負額の現状と通知順も代表値で確認する。既存の建設・車両・購入の拒否状態、期限年境界・達成優先・フリーモード期限・週次途中失敗は再利用する。移行後は例外内の値と後のモデル変更からの不変性、既存2文章を検査する。UIのcatch接続は全経路の静的確認で検証し、実ダイアログは未操作。

仕様・影響レビュー（Astra、同日）：設計へ進行可、追加指摘なし。2生成・6catchを確認。支払の通知、期限優先、非正期限、実行中期限、全画面のタイトル/閉じる/終了処理を維持する。stringコンストラクタ削除は外部派生例外のbase(message)にも影響することを記録する。

## 設計

Model/GameFailure.csへsealed record MoneyShortageFailure(long RequestedAmount, long AvailableMoney)、GameOverReason { DeadlineExceeded }、sealed record GameOverFailure(GameOverReason Reason, int Year, int DeadlineYear)を追加する。可変参照・不足額導出・目標状態は含めない。

既存MoneyShortException/GameOverExceptionのコンストラクタを各record引数へ置換し、get-only Failureへ保存、内部Messageは英語にする。例外型と継承は維持、stringコンストラクタ互換は終了する（外部派生のbase(message)も対象）。旧文章をModelへ残す互換コンストラクタは追加しない。

SpendMoneyは比較前にMoneyを局所値へ保存し、同じ条件でRequestedAmount/AvailableMoneyを保存して拒否する。成功のMoney -= amountと通知は維持する。期限条件はそのまま、拒否時にYear/MYearだけ保存する。

View/GameFailureFormatterに各recordのFormat overloadを置く。資金は既存固定文、期限はDeadlineExceededから既存固定文を生成する。未知終了理由はArgumentOutOfRangeExceptionとする。全6catchの文言引数だけ接続し、例外捕捉範囲・タイトル/アイコン・確認・Close・ForceExitは維持する。新しい操作サービスや通知返却APIは導入しない。

既存支払・期限・途中失敗テストは再利用し、新規現状固定を型とデータへ移行する。資金/期限snapshot不変性と表示2文章・未知理由を追加する。UIの全catch接続を静的確認する。

設計レビュー（Astra、同日）：承認、実装開始可、修正必須指摘なし。既存型と判定順・通知・全catchの表示条件を維持し、少数のrecord/formatterに限定する構成は責務分離・可読性・保守性・過剰さの観点でも適正。

## 実装と検証

2生成箇所をデータ付きの既存例外へ移行し、全6catchをformatterへ接続した。差分で捕捉範囲・タイトル・アイコン・Close/ForceExitが変わらないことを確認した。

変更前の7件成功。移行後は支払の拒否/成功/境界・通知・負額、目標有無と実行期限を9件で確認し、資金のlong極値と後の資金/年/期限変更からの不変性を含めた。表示2文章と未知終了理由を3件で検査した。既存の建設・車両・購入拒否、期限境界・達成優先・フリーモード期限・週次途中状態も含め全488件成功、失敗・スキップ0（2026-10-07）。実ダイアログは未操作。

採用判断はADR 0017。週送りtry範囲とフリーモード終了文言の判断は別途残す。SpendMoneyの負額が資金を増やす現状は不具合候補として記録し、今回変更しない。

仕上げレビュー（Astra、同日）：承認、修正必須指摘なし。2生成・6catchの接続と挙動維持、判定時の値・不変性、既存テスト再利用を確認。責務分離・可読性・保守性・過剰さも適正。全488件成功は親の実行結果で確認し、レビュー側では再実行していない。
