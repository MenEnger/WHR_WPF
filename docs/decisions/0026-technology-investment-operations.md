# ADR 0026: 技術投資額の設定を本体操作へ移す

日付: 2026-10-08
状態: 採用。仕様・影響、設計を別々にAstraレビュー済み。

## 背景

ADR 0006 / Issue #28の調査で、投資額を設定する5つのVM setterが本体のpublic fieldを直接書き換えている。既存週次処理は画面なしで動くが、投資設定の責任がUIに残る。

## 決定と理由

GameInfoへ部門別のSetSteamInvestment/SetElectricInvestment/SetDieselInvestment/SetLinearInvestment/SetNewPlanInvestmentを追加し、UIのsetterを接続する。既存enumを使い、LinearだけInvestmentAmountLinearEnum、他はInvestmentAmountEnumとする。対応部門への代入だけを行う。

今回は投資可否・定義値の検査・週次支払/開発/停止・通知なしという現状の契約を変更しない。Canは表示用の照会として維持し、新しい拒否や停止を導入する場合はユーザーの仕様判断と回帰検証を伴う別変更とする。現在ない設定時通知・イベント・支払を追加しない。

## 代替案

- UIからの直接書換を継続：別UI/ハーネスもfieldを変更して操作する必要がある。
- 新しい部門enumと汎用setter：既存enumのリニア型差を扱う追加規則が必要となり、5操作の移管に対して過剰。
- 全部門額の一括置換：単一部門の操作にread-modify-writeが必要で、他部門を上書きする範囲が広がる。
- Can/未知enumガードを同時追加：不可時の停止・通常ディーゼル前提等を責務移動だけで決めてしまう。別に検討する。

## 影響・検証

公開操作の追加のみ。public weeklyInvestment fieldは初期化・週次処理・既存試験と互換を維持し、今回削除しない。Core/Scenario/UIの依存はADR 0024のまま。#28の全入力検証は未完了。

部門ごとの代入・0/未定義enum/Can=false、資金/累計/技術・他部門保持と通知なしをCoreで検査する。実Window/VMのsetter/getter接続をWPFで検査し、終了時のlistener解放を維持する。週次判定は既存試験を再利用する。結果は[実装文書](../design/technology-investment-operations.md)へ記録する。
