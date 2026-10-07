# 路線設備の操作拒否を表示から分離する

2026-10-07。Issue #26の第四段階。

## 仕様検討

SpeedUp、UnElectrify、Electrify、NarrowGauge、ExpanseGauge、AddLane、ReduceOrRemoveLane、ChangeTaihiの冒頭の操作不可を、理由コードと発生時点の対象・必要状態で伝える。8つの現行文章はViewで生成する。

既存Canメソッド・条件順・支払・状態変更・運行解除を維持する。操作不可の細かい原因の再分類はせず、既存の操作単位の拒否をコード化する。公開操作のシグネチャとCan APIは維持し、具体例外型をInvalidOperationException派生型へ変更する。

建設引数の不正、内部のレーン数異常、資金不足、一般引数誤り、編成不足・割当、週次例外は今回対象外。未知入力や費用式の補正を混ぜない。

## 影響確認

ReformViewModelから設備操作7種、TaihisenChangeViewModelから待避線変更、路線ダイヤ設定VMから速度向上が呼ばれる。系統VMの速度向上呼出はコメントアウトされており移行対象外。Reform/路線ダイヤの例外案内はViewModelBaseで表示されるため専用catchを追加する。待避線画面は独自に資金不足だけを捕捉し操作拒否を未捕捉とする現状であり、今回catchを追加せず別の不具合候補へ記録する。CanExecuteや確認・見積・画面終了は変更しない。

厳密なInvalidOperationException型比較とMessage依存は互換性変更となる。基底型でのcatchと操作シグネチャは維持する。内部・引数・資金例外を機械的に同じ拒否型へ変えない。

## 検証計画

冒頭拒否8種の文章と発生順・未変更状態を事前に確認する。速度/改良回数・待避線レーン数の境界、電化/軌間/軌道の代表条件、資金不足/内部例外の区別を検査する。成功と運行解除は既存テストを再利用し、同じ操作の検査を重複して増やさない。移動後は理由と時点値・後の変更からの不変性を確認し、8文章はUI側で検査する。実ダイアログ未操作。

仕様・影響レビュー（Astra、同日）：待避線画面の独自catchを確認し、拒否未捕捉を今回変更しない旨を修正した。速度改良回数==5、現在速度による上限検査、未知Type、nullable電化、要求待避線enum未検査を維持する。gameInfoの一律null検査を追加せず、許可後の支払/変更/通知/解除途中での内部失敗も現状のままとする。指摘を解消して設計へ進む。

## 設計

LineEquipmentRejectionReasonにSpeedUpUnavailable、UnElectrifyUnavailable、ElectrifyUnavailable、NarrowGaugeUnavailable、ExpanseGaugeUnavailable、AddLaneUnavailable、ReduceUnavailable、TaihiUnavailableを定義する。

sealed record LineEquipmentFailure(Reason, LineName, IsExist)へ、必要な段階のnullable init値だけを保存する。速度拒否はBestSpeed/ImprovementCount/Type/Gauge、電化・非電化はType/IsElectrified、軌間変更はType/Gauge、待避線はLaneCount、増設・削減は共通IsExistのみ。他はnullとし、GameInfoや可変Line参照を保持しない。速度上限定数は既存の公開Line定数を使え、今回重複保存しない。

Lineの8冒頭分岐で、同じCan判定の後にLineEquipmentRejectedException(Failure)を投げる。例外はInvalidOperationException派生、Failure取得専用、Messageは英語内部診断。分岐の近くで値を直接作り、汎用helperや新しい可否APIは追加しない。可否の細かい原因を再判定しない。

View/LineEquipmentFailureFormatter.Format(Failure)が8旧文章を作り、未知理由はArgumentOutOfRangeExceptionとする。ViewModelBaseに一般InvalidOperationExceptionより前の専用catchを追加する。他画面の実行/確認/CanExecuteや未捕捉経路は維持する。

検証は新拒否型とsnapshotへ移行、既存成功/資金不足のテストを再利用する。ReformのCanExecuteからモデル可否への接続と速度操作拒否の伝播を代表検査し、実ダイアログは未操作と明記する。

正式設計レビュー（Astra、同日）：実装開始可。操作別に定義した保存項目を一貫して設定し、サブ理由による取得の再分類をしない。1enum・1record・1例外・1formatterは責務分離と可読性・保守性の観点で適正。既存成功/資金不足テストを再利用する。

## 検証結果

製品変更前の基準13件成功。移動後は拒否・境界・内部例外・snapshot14件と、文章8種・未知理由・Reform接続の10件を追加し、全437件成功（失敗・スキップ0）。既存の成功費用・設備変更・編成解放・資金不足テストは再利用した。

実ダイアログ未操作。ReformのCanExecuteと速度拒否伝播を自動検証し、ViewModelBase専用catchの順序とその他の処理は差分で照合した。待避線画面の未捕捉は維持し、bug-notesへ記録した。

仕上げレビュー（Astra、同日）：修正必須指摘なし。8冒頭拒否と表示境界に変更を限定し、既存成功/費用/解除を再利用した検証は適正。責務分離を保ち、条件と保存値を近くに置く構成は可読性・保守性・過剰さの観点でも問題なし。
