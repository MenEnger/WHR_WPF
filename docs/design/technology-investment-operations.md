# 技術投資設定を本体操作へ移す

2026-10-08。Refs #28 / #23。PR #56の後続。[全操作調査](common-operation-audit.md)の投資設定を対象とする。

## 仕様検討

TechnologyDevelopViewModelの5つのsetterがweeklyInvestmentの各fieldを直接変更している。本体操作を追加しUIからそこへ接続する。部門別の値設定だけを移し、Can*可否、未知enumの許容、週次支払/開発/停止、金額表示は現状を維持する。Canガードや設定後不可となる投資の停止、通常ディーゼル前提は別の仕様判断としてユーザーへ確認中。

操作は既存enumの値を受け取り、指定部門だけを変更する。設定時点では資金/累計投資/技術値を変更せず、現在ない通知やイベントを追加しない。リニアだけ別enumを使う現状を維持する。既存public weeklyInvestment fieldは解析・初期化・週次処理・既存試験の互換性のため今回削除しない。全入力検証や#28完了ではない。

## 影響確認

製品setterはTechnologyDevelopViewModelのSteamInvest/ElectricInvest/DieselInvest/LinearInvest/NewPlanInvestの5箇所。他の書込はCoreのモード初期化と週次停止であり今回変更しない。UIのgetterは本体の現値を読む。GameInfoLogic.Can*は表示用照会、GameInfoWeekly.ChargeWeeklyTechnologyInvestmentsは資金/累計へ反映、TechnologyDevelopWindowは選択肢バインドと表示。蒸気IsEnabledの誤名はbug-notesの別件として保持する。

CoreのInvestmentAmountEnum/InvestmentAmountLinearEnumと、GameInfo.weeklyInvestmentのstructを使用する。本体操作の名前と型は設計で決める。新しい部門enumや共通結果型、全額一括置換、facade/プロジェクトは具体的な必要がない。

## 仕様・影響レビュー

Astra承認。5部門の単純代入移管、公開fieldの互換維持、未決入力規則と週次処理の分離に修正必須指摘なし。

## 設計

GameInfoLogicへSetSteamInvestment/SetElectricInvestment/SetDieselInvestment/SetLinearInvestment/SetNewPlanInvestmentを追加する。既存enumを受けるvoid操作とし、LinearのみInvestmentAmountLinearEnum、他4部門はInvestmentAmountEnumを使う。各操作は対応するweeklyInvestment fieldへの代入だけを行い、5VM setterをそれぞれ接続する。field getter/モード初期化/週次停止/バインドは変更しない。

5メソッドを採る理由は既存の部門名と型をそのまま使い、新たな部門enumやリニアの型変換規則、全額read-modify-writeによる他部門上書きを作らないため。後の入力規則はこの操作へ実装できるが、今回は無検査の現状を保持する。

Coreの5部門DataRowで、別部門に異なる初期額を入れ、指定部門だけへの設定、0/未定義値/Can=falseでの現行代入、資金・累計・技術値とPropertyChanged非発生を確認する。各Factory/週次数式を再検証する試験を増やさない。WPFはSTA上の実Window/内部VMを使用して5つのsetter/getterの異なる値と部門対応を確認し、Windowを閉じて既存listener寿命を維持する。実画面クリック/ダイアログの試験は含まない。全solutionを独立出力で検証する。

## 設計レビュー

Astra承認。5型付き操作とLinearの型差、他部門上書き防止、Core5ケース/UI接続1ケース、Window.Closeによるlistener解放に修正必須指摘なし。


## 実装・検証

5本体操作とVM setterを設計どおり接続。PR #56時点の全538件成功を事前の状態確認として利用した。独立BaseOutputPath、UseSharedCompilation=false、solution test -m:1で全544件成功（Core299/Scenario84/WPF161、失敗・スキップ0）。追加はCore5部門とWPF接続1件。未知値/Can=falseの受理はCurrentBehaviorとして記録し、採用した新規入力ルールではない。

TRXはartifacts/technology-investment-results。NuGet脆弱性情報取得NU1900警告は継続するが復元・ビルド・テストは成功。WPFは実WindowのDataContextとVM setter/getter、終了時のCloseを使った接続検査。画面のクリック/操作性や蒸気の既知バインド誤名はこの検査の対象外。

## コードレビュー

Astra承認。5部門の単純代入とVM接続、Core5ケース/WPF1ケース、実Windowの後処理、現行入力規則の分類、可読性・保守性・過剰さに修正必須指摘なし。全544件のTRXを確認済み。

