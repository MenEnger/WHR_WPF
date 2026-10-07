# 路線Captionの表示境界とIssue #27完了確認

2026-10-08。Issue #27の最終段階。

## 仕様検討

路線選択一覧の表示は現状の「路線名 半角空白 始点名～終点名」を維持する。Model.LineはName/Start/Endを返し、表示の組立てはUI側へ移す。選択から得る元のLine参照と画面遷移に使うデータは維持する。表示名全体を対象とする文字入力検索も維持する。同名の路線でも区間と参照で区別できる必要がある。

GameInfoのモード適用における不整合例外は内部診断であり、従来の文章と路線の識別情報を維持する。この診断だけのためにモデルの汎用表示Captionやモデルから表示層への依存を残さない。モード失敗時の途中状態と捕捉経路は今回変更しない。

## 影響確認

Line.Captionの製品利用はLineInfoPageのLineList DisplayMemberPathとGameInfoのシナリオダイヤ設定診断の2経路。DiagramInfoPage.xamlのCaption指定はcode-behindでNameへ上書きされ、Lineではないので変更しない。他のCaptionは選択肢の表示部品であり、今回の路線Captionと区別する。

LineInfoPageはgameInfo.linesをItemsSourceへ直接渡しSelectedItemをLineに設定、DropDownClosedで選択路線のページへ移る。表示用の項目へ置換する場合、選択から元のLine参照を取り出す経路と画面遷移を同時に移行する必要がある。元のLineデータと参照は維持する。Nameと駅名はモデルのデータとして残す。名称は通知付きsetterではなく、現状のCaptionも名称変更で通知しない。今回名称編集機能や通知の追加は行わない。

公開Line.Captionの削除は外部のソース/バイナリ/reflection/XAMLバインドへの互換性変更。製品内の全参照は移行する。内部診断は同じ生の路線・駅名を組み立てて従来文を維持し、UI用の表示APIを共用しない。計算・保存・運行にはCaptionの参照は見つからない。

## 検証計画

移動前は旧Captionの表示とモード不整合の例外文章・途中状態を確認する。実LineInfoPageの表示と選択を基準検証し、移動後は実XAMLで同じ名称/空文字/null名称と日本語・同名路線の別区間・選択参照を確認する。無効なStart/Endの補正や入力仕様は今回追加しない。全試験とModelから表示依存がない静的確認を行う。

#27の完了条件は金額/enum/Caption、値による選択、計算属性維持、数式/単位/符号維持、表示と計算試験の分離を今回の最終結果と過去のマージPRへ照合する。#24の投資表示/符号と不具合候補は未修正のまま別範囲である。

## 仕様・影響レビューでの補足

Astraの指摘により、DisplayMemberPathは見た目だけでなくComboBoxの文字入力検索にも使われることを確認した。路線名に続く区間を含む接頭辞で、同名の別路線を選べる契約を基準検証へ加える。

[WPF TextSearchの実装](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/TextSearch.cs)では、検索は項目自身のTextSearch.Text、TextPath/DisplayMemberPath、ToString等の順で対象文字を決める。ItemTemplateだけへの置換では元のLineから同じ合成文字列を取得できない。Modelへの表示プロパティを残さず検索を維持するには、UI側の表示項目を導入し、選択から元のLineを取り出す経路も更新する必要がある。共有型の属性読取を上書きする登録機構や独自検索エンジンは追加しない。

gameInfo.linesはListで、ページは現在の一覧を表示する。表示項目はページ生成時に作り元のLineを参照する。路線名・駅名は新しいモデル複製へコピーせず、その参照から現在値を読む。今回動的な一覧編集/名称編集や通知の新仕様は追加しない。初期選択、同名別区間選択、文字入力による選択と遷移に使うLine参照を検証する。

## 仕様・影響レビュー結果

文字入力検索と表示項目の必要性を補足した後、Astraが仕様・影響を承認した。基準のCaption4ケース・モード設定5ケースは成功し、実WPFの表示/選択と文字入力検索2件も成功した。維持対象は一覧順序・元Line参照・選択/検索/遷移で、ItemsSource自体の同一参照は新しい境界の契約にしない。

## 設計

ViewModel/Componentに小さなLineSelectionItemを置く。コンストラクタで元のLineを保持し、読取専用Lineプロパティと、その現在のName/Start.Name/End.Nameから組み立てるCaption getterだけを持たせる。値やモデルをコピーせず、通知・共通基底・独自Equalsを追加しない。路線の選択は元Lineの参照で扱う。

LineInfoPageは現在のgameInfo.linesを同じ順序でLineSelectionItemへ投影したListをItemsSourceに渡す。XAMLのDisplayMemberPath="Caption"を維持しSelectedValuePath="Line"を追加する。初期選択とDropDownClosedでの読出しはSelectedItemからSelectedValueへ移し、元Lineでページを生成する。この表示項目は表示と文字入力検索の両方に必要な一つの境界で、共通選択サービスや新しい操作APIは作らない。

Line.Captionを削除する。GameInfoの診断はその箇所だけでName/Start.Name/End.Nameを従来と同じ区切りで記述する。モデルから表示部品は呼ばない。診断の文字列・例外型・発生順・失敗時の途中状態は維持し、新しい例外捕捉は追加しない。

Captionの固定期待4ケースはLineSelectionItemのgetterへ移行する。実ページ試験はItemsSource内の順序と各元Line、SelectedValueのLine参照、画面表示、同名別区間の選択、文字入力検索を確認する。可能な非表示FrameでDropDownClosed後のLineInfoPage/VMが同じ元Lineを受け取る遷移も検証する。元のモード失敗試験へ追加した診断文assertと既存の途中状態assertを維持する。#27完了条件の照合はPR #46～#48と本段階の根拠を使用する。

## 設計レビュー

Astraが正式設計を承認した。参照保持とライブgetter、表示と検索の一つの境界、SelectedValueによる元Lineの受渡し、内部診断の維持に修正必須の指摘なし。基準11件成功の確認後、実装へ進んだ。

## Issue #27完了条件の照合

| 条件 | 根拠 |
| --- | --- |
| 金額・enum名・路線Captionを表示側へ移す | 金額はPR #46/ADR 0020、enumはPR #48/ADR 0022、本段階でCaptionをLineSelectionItemへ移す。モデルに表示側の参照を追加しない。 |
| 値で選択肢を照合し文言変更/重複を実バインドで検証 | PR #47/ADR 0021の比較5件と実WPF3件。路線は元Lineの参照で選び、同名別区間と検索・遷移を本段階で確認。 |
| 計算用属性とデータ文字列を保持 | PR #48でComfortLevel/GetAttributeを維持。路線名・駅名・車両名・作者説明は削除しない。 |
| 移動で数式・単位・符号を変えない | PR #46の固定期待12件、#48の69表示名。#24の符号欠落/投資意味と不具合候補を変更しない。 |
| 表示試験とゲーム計算試験を区別 | 専用formatter/表示名/表示項目試験と実WPF試験を配置し、既存の純粋なモデル状態変更・計算試験を再利用する。 |

内部シナリオ診断、データ名称、日本語コメントは表示専用APIと区別し本体に残す（親 #23の調査で合意した除外範囲）。既知の単位重複や巨大金額の表示、選択肢にない保存線数等はdocs/bug-notes.mdへ分離しており、#27の責務移動の完了と修正済みを混同しない。最終試験とレビューが通ればCloses #27、親はRefs #23を指定する。

## 実装・検証結果

表示項目・SelectedValueによる選択/遷移を追加し、ModelのCaptionを削除した。内部モード診断は生の名前を同じ位置で組み立て、文章と途中状態を維持した。移動前の基準11件（Caption4・実WPF2・モード5）成功、移動後は表示項目4・実WPF3を含む全533件成功、失敗0・skip0（隔離出力artifacts/line-caption-tests、UseSharedCompilation=false）。

実XAMLの文字表示と同名別区間の選択、TextInputEventによる「同名線 京」の検索、非表示Frameで選択→DropDownClosedの実handler→ページ遷移後のVM/lastSeenLine/SelectedValueが同じ元Lineを受け取ることを確認した。固定診断文と既存モード途中状態assertも成功。Model/Utilから表示依存とCaption参照はなく、他の表示部品のCaptionは維持した。判断はADR 0023。手動のマウス操作は未検証。

## 仕上げレビュー

Astraが差分・網羅性・可読性・保守性・過剰さ、文書と5完了条件を確認し承認した。修正必須の指摘なし。Closes #27とRefs #23が妥当と確認済み。新しい表示項目は検索を維持する具体的な必要性に限り、既存WPF機構と元モデル参照で選択・遷移を接続している。
