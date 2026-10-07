# 金額表示を計算Utilから分離する

2026-10-08。Issue #27の第一段階。

## 仕様検討

LogicUtil.AppendMoneyUnitと私有ConvJapaneseNumeralを表示側へ移す。10万円単位から円・万・億・兆へ変換する現行文章、Math.Absによる符号欠落、unchecked乗算と桁切捨て、Math.Absの極値例外を維持する。負値表示と投資累計の意味は #24の別判断とする。既知の確認文の「円拾万円」重複も今回修正しない。

enum表示名・Line.Caption・選択肢Equalsは後続段階に分け、このPRでは金額表示だけを扱う。

## 影響確認

利用箇所はViewModelの表示/確認文のみで、Model/計算からの呼出はない。TaihisenChangeWindowTestsには旧型名からAppendMoneyUnitをreflectionで探す参照がある。全呼出と表示テストを新しい表示側へ接続する。LogicUtil自体はinternalで、publicメソッドの削除は通常の外部assembly向け公開型APIの削除ではない。同一assembly・友達assembly・reflectionで旧型を参照する利用側は移行が必要。私有ConvJapaneseNumeralの互換は不要。

表示される文・確認/実行フロー・金額計算・バインド用プロパティ名は維持する。既存表示回帰試験を再利用し、全画面の同じ金額変換試験を重複しない。

## 検証計画

変更前に0・正負・万/億/兆の境界と混合桁、16桁以上の桁切捨て、乗算overflow代表・Math.Abs例外を必要最小限で固定する。移動後に期待値を維持し、旧LogicUtilとModelに金額表示処理・参照が残らないことを検索する。既存VM表示と待避線見積も含め全体検証を行う。実ダイアログは未操作。

仕様・影響レビュー（Astra）：承認、設計へ進行可。全34製品参照はVMに限定、待避線試験のreflectionも移行対象。メソッド入口の10万円刻みで代表値を固定し、私有1円変換の全境界を重複試験しない。Math.Abs例外は乗算後long.MinValueとなる入力で検査する。

## 設計

View/MoneyDisplayFormatter.csにpublic static MoneyDisplayFormatterを置き、public static string Format(long amount)で既存AppendMoneyUnitの本体をそのまま実行する。私有ConvJapaneseNumeralも同クラスへ移す。丸め・桁・符号・unchecked乗算を変更しない。

LogicUtilから両メソッドを削除し、互換wrapperは作らない。VMの全34呼出をMoneyDisplayFormatter.Formatへ置換し、必要なView usingを追加する。型修飾が変わるだけでプロパティ名・表示文・確認・実行は維持する。

待避線表示試験のreflection経由を新formatterへの直接呼出に変更する。変更前のgolden試験は呼出先だけ移行し、正負・通常境界・混合桁・上位切捨て・乗算極値の公開入力を確認する。計算用試験とは別のDisplay分類を付ける。既存のVM表示・WPFバインド試験は維持する。

採用判断はADR 0020、残りのenum/Caption/Equalsは後続。新しいインターフェースや汎用整形サービスは導入しない。

設計レビュー（Astra）：承認、実装開始可。単一formatterと既存本体の移動、34参照と試験更新に限定する構成は責務分離・可読性・保守性・過剰さの観点でも適正。親の追加確認でLogicUtil型がinternalである点を互換性記録へ補足した。

## 実装と検証

計算用Utilから金額表示2メソッドを削除し、Viewのformatterへ移動した。VM13ファイルの全34呼出を更新、待避線試験と固定期待値を直接参照へ接続した。製品/テストに旧AppendMoneyUnit参照なし、Model/Utilに新formatter参照なしを検索で確認した。

変更前12件成功。移動後も同じ12件と既存VM/WPF回帰を含め全507件成功、失敗・スキップ0（2026-10-08）。表示試験はDisplay分類。実ダイアログ未操作。巨大値の桁切捨て/overflowはdocs/bug-notes.mdへ現状と確認範囲を記録し、今回補正しない。判断はADR 0020。

仕上げレビュー（Astra）：承認、修正必須指摘なし。旧本体と全34参照の挙動維持、12固定期待値と既存VM/WPF回帰の網羅性、internal型の互換性説明を確認。単一formatterは責務が明確で、過剰な抽象化や保守負担なし。全507件成功は親の実行結果を参照した。
