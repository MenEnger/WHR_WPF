# ADR 0022：enum表示名を表示層に置く

- 状態：採用
- 日付：2026-10-08
- 関連：Issue #27

## 背景

モデルのゲームenumがDisplayAttributeで日本語の表示名を持ち、公開ToNameがその属性を読む。表示の変更をゲームの計算・保存値から分離する。

## 判断

全69表示名をView.EnumDisplayNames.ToNameへ型/値のswitchとして移す。数値・識別名・表示名は維持する。座席のComfortLevelAttributeと汎用GetAttribute、計算用拡張はModelに残す。

switch以外は旧属性読取を使用し、表示属性なしはToString、外部enumのDisplay属性ありはNameをそのまま返す。Name=nullも維持し、リソース解決を追加しない。未知値・nullの旧例外を今回改善しない。

## 代替案

- 属性とModel.ToNameのwrapperを残す：本体から表示の責務が除けず、表示側への依存も残るため不採用。
- 表示用のenumを複製する：値の同期・変換を二重管理する必要がないため不採用。
- 汎用表示サービス/注入/辞書登録機構：単一の既存表示名対応で足りるため不採用。

## 影響

公開ToNameはnamespaceと所属型が変わり、利用側はView.EnumDisplayNamesを参照する必要がある。公開enumのDisplay属性削除も別の互換性変更であり、reflectionやGetAttribute<DisplayAttribute>()で表示名を読む外部利用は新しい表示側APIへ移行する。製品の呼出はVM/部品に限られ、10ファイル26行28呼出を確認した。多くは既にViewを参照しており、追加usingが必要なのはTaihiViewComponentだけである。

GetAttributeの公開APIと座席計算の所属・値は維持する。表示属性のない定義enumと外部enumの従来動作も維持する。駅名・車両名などのデータ文字列とLine.Captionはこの段階の変更対象外。移動前基準11件、移動後全526件で表示/計算/実バインドの既存回帰を検証した。詳細はdocs/design/enum-display-boundary.md。
