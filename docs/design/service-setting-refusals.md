# 路線・系統の運行設定拒否を表示から分離する

2026-10-07。Issue #26の第六段階。

## 仕様検討

Line.SettingCompositionの路線状態と事前編成不足、KeitoDiagram.SettingCompositionの区間状態・本数上限・事前編成不足の5拒否を構造化する。直接/系統の対象種別、拒否理由、対象名と要求本数、事前不足では必要編成数・比較に使った余剰数・既存の不足計算結果を保存する。可変モデルを保持せず、3つの現行文章はUIへ移す。

条件・引数検査順・適合検査・頻度判定・計算式・旧割当解除とUseの順序・途中状態は維持する。事前不足の余剰二重評価の問題は修正しない。計算結果を実際の不足数と再解釈せず、そのままの名称で保持する。

比較の左辺HeldUnitsを先に読み、右辺CalcMissingCompositionsを既存位置・回数で呼ぶ。右辺内部の再読込も変更せず、比較に使用した値だけを保存する。未知ICompositionのName/Price等を追加取得しない。

## 影響確認

実行元は路線/系統ダイヤ設定VM、表示はViewModelBase。既存の選択・CanExecute・確認を変更せず、一般InvalidOperationExceptionより前の専用catchで文章を作る。編成適合拒否・Use段階のStockShortageException・内部例外は今回置換しない。

操作シグネチャと基底catch互換は維持。具体例外型と日本語Messageの互換は終了する。既存テストの初期拒否の期待だけを更新し、旧割当解除後のStockShortageException期待は維持する。

## 検証計画

5拒否箇所と優先順、成功との境界、拒否状態を変更前に固定する。移動後は理由と比較時点の値・不使用null・不変性、左辺/右辺の読込順を代表検査する。既存の割当成功と途中状態を再利用し、全組合せ試験は追加しない。現行3文章をViewで検査する。実ダイアログ未操作。

仕様・影響レビュー（Astra、同日）：設計へ進行可。必要編成数の計算→比較左辺HeldUnits→右辺CalcMissingの順を維持し、右辺を代算しない。3数量は同時点の整合値ではなく既存評価順の観測値。unavailable/frequencyでは数量を未設定とし、系統の頻度再評価も追加しない。DiagramType.Noneでの後続ArgumentException、適合拒否、解除後Use拒否は既存契約のままとする。

## 設計

ServiceSettingTarget（Direct、Through）とServiceSettingRejectionReason（Unavailable、FrequencyExceeded、StockUnavailable）を定義し、sealed record ServiceSettingFailure(Target, Reason, TargetName, RequestedRunningPerDay)をModelに置く。初期不足だけint? RequiredUnits/AvailableUnits/CalculatedMissingUnitsをinit保存し、他の拒否ではnullにする。比較結果を実際の不足数へ再計算せず、数量は既存評価順で得た値とする。

ServiceSettingRejectedExceptionはInvalidOperationException派生、Failure取得専用、英語内部Message。対象名はLine/Keito自身のNameだけでnullも許容、可変モデルと未知編成getterを保持・追加しない。

Line/Keitoの5既存拒否箇所を置換する。不足比較では既存の必要数計算後に左辺HeldUnitsを局所保存し、既存CalcMissingの呼出結果を別の局所値へ保存、同じ比較で拒否する。計算回数・後続解除/Use・系統diagram復元を維持する。

View/ServiceSettingFailureFormatterはTargetとReasonから既存3文章を作る。DirectのFrequencyExceededや未知値など未対応組合せはArgumentOutOfRangeExceptionとする。ViewModelBaseで一般InvalidOperationExceptionより前に専用型を捕捉する。VMの可否・確認・実行を変更せず、共通割当サービスや新検査APIを追加しない。

既存テストの初期拒否だけ期待型を更新し、成功・適合拒否・途中Use拒否を再利用する。新規は5拒否の値/状態/優先・数量の取得時点・不変性と文章に絞る。実ダイアログは未操作。

設計レビュー（Astra、同日）：実装開始可、修正必須指摘なし。左辺の余剰と右辺の計算値を独立した局所文で取得する設計を確認。共通割当サービスを追加せず、必要な表示境界と5拒否に限定する構成は可読性・保守性・過剰さの観点でも適正。

## 実装と検証

初期5拒否を専用例外へ置換し、Viewのformatterと共通VMのcatchを接続した。既存初期拒否3件の期待型だけ更新し、旧割当解除後のUse拒否は変更していない。

変更前の現状固定13件成功。移行後は拒否と判定値・優先順・状態・数量取得順・不変性を15件、表示5組合せと未対応3組合せを8件で確認した。既存の成功・適合拒否・途中失敗試験も含め全476件成功、失敗・スキップ0（2026-10-07）。実ダイアログは未操作。

系統の頻度判定でNone等のダイヤが改善されずループする可能性をコード上で発見し、docs/bug-notes.mdへ記録した。今回の挙動変更には含めていない。採用判断はADR 0016。

仕上げレビュー（Astra、同日）：修正必須指摘なし。既存評価順・5拒否の範囲・表示接続・状態保持とテストの網羅性を確認。責務分離・可読性・保守性・過剰さも適正。全476件成功は親の実行結果に基づき、レビュー側では再実行していない。
