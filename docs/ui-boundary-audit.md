# ロジックとUIの境界調査

2026-10-08補足：以下は調査当時のAPI・行番号・提案であり、全体の進捗は親Issue #23、通知分離の詳細はIssue #26、通知/想定内拒否分離の最終範囲は[週次契約の設計](design/weekly-failure-contract.md)と[ADR 0019](decisions/0019-weekly-partial-failure-contract.md)を参照する。T11のうちGameInfo.ApplyModeSettingで発生するCannotContinueExceptionは、ModeSelectPage.NavigateGameのSelectedMode設定から未捕捉で伝播し、MainMenuPageの読み込み診断保存へ接続されない。読み込み段階の診断とは経路が異なる。

表示専用責務と選択肢の分離はIssue #27へ集約する。金額・enum表示名・選択肢同一性・路線Captionの到達点と検証は[表示境界の完了照合](design/line-caption-boundary.md)とADR 0020～0023を参照する。下記のT12～T14と選択肢の比較記述は起票時点の調査である。

2026-10-06。対象は `codex/original-state-logic-audit` の作業ツリー。調査記録であり、分離の実装や仕様変更は行っていない。ローカルの待避線修正案も含む。

採用した到達点と検証基準は[ADR 0006: UI非依存のゲーム本体](decisions/0006-ui-independent-game-core.md)に記録する。以下の調査結果と、採用判断・将来の実装を区別する。

## 対象と調査方法

`Model` と `Util` の全C#ファイルから、文字列の戻り値、文字列補間、例外メッセージ、通知追加、表示名属性、WPF型、画像、ダイアログ、アプリ終了、ログ出力を検索した。生成された情報の受け取り側を `View`、`ViewModel`、テストで追跡した。

以下の行番号は調査時点のもの。改修時にはメソッド名でも確認する。表の分離案は提案で、採用済みの実装ではない。

## ロジックから表示文言が流れる経路

| ID | 生成元 | 受け取り側 | 確認した内容と分離案 |
| --- | --- | --- | --- |
| T01 | `Model/GameInfoLogic.cs:159` `NextWeek` | `ViewModel/GamePageViewModel.cs:111,129` | `List<string>` の通知を返し、UIは改行・区切りを付けてMessageBoxに表示。イベント種別とデータを返し、順序と発生回数を保持してUIで文章化する。 |
| T02 | `Model/GameInfoWeekly.cs:384` `CompleteWeeklyEngineDevelopment` | T01経由 | 蒸気・電気・ディーゼル・リニアの9か所で通知追加。初開発、速度上限向上、費用低下を区別し、技術と変更前後の値を返す。特に費用低下の文だけでは変更量が得られない。 |
| T03 | `Model/GameInfoWeekly.cs:455` `CompleteWeeklySpecialTechnologyDevelopment` | T01経由 | 新企画10種類の通知に効果説明と改行を埋め込む。技術IDと必要な変化のデータを返し、説明文は表示側へ。同じ週の複数開発を保持する。 |
| T04 | `Model/GameInfoLogic.cs:186` `NextYear` | `AdvanceWeeklyCalendar` → T01 | 蒸気設定終了、戦時開始、戦時終了の3か所で通知追加。年次イベントとして返す。通知と蒸気・貨物の状態変更を別々に検証する。 |
| T05 | `Model/GameInfoWeekly.cs:548` `CheckWeeklyGoals` | T01経由 | 達成・フリーモード移行の通知1か所。状態遷移を明示するイベントにし、文言と達成状態を分離する。期限参照の不具合はIssue #7として別に扱う。 |
| T06 | `Model/GameInfoLogic.cs:55` `CheckCreateVehicle` | `ViewModel/Vehicle/VehicleDevelopViewModel.cs:160` `Msg` → `VehicleDevelopWindow.xaml` | `(bool, string msg)`。名前未入力、動力未開発、速度超過、蒸気使用期限、座席・傾斜装置の未開発を日本語で返す。複数の速度超過が同じ「速すぎます」。理由コードと動力・上限などを返す。`DevelopVehicle:114` は詳細理由を捨て一般的な例外文にするため、判定と実行の理由統一は、その差を現状テストで記録した後のAPI整理候補。移動だけの段階では既存差を保持する。 |
| T07 | `Util/LogicUtil.cs:306` `CompositionFactory.CheckMakeComposition` | `ViewModel/Vehicle/CompositionMakeViewModel.cs:100` `ErrorMsg` → `CompositionMakeWindow.xaml` | 名前なし、車両なし、軌間・軌道・動力・傾斜の不一致、速度不足を `(bool,string)` で返す。`CreateComposition:352` は同じ文をInvalidOperationExceptionに載せる。理由コードと不一致項目・速度を返す。成功時の `null` とT06の空文字の差の統一は、その差を現状テストで記録した後のAPI整理候補。移動だけの段階では既存差を保持する。 |
| T08 | `Model/Line.cs:795` `ValidateCompositionAcceptable` | `LineDiagramSettingViewModel.cs:34`、`KeitoDiagramSettingViewModel.cs:37` | リニア・軌間・電化・蒸気使用期限の違反をCompositionNotAppliedExceptionの文で返し、画面のErrorMsgへ直結。系統は `KeitoDiagram.ValidateCompositionAcceptable` 経由で各路線を検証する。違反理由と対象路線・編成のデータを保持する。 |
| T09 | `Model/Line.cs` の建設・改造・運行操作、`Model/KeitoDiagram.cs:248` の割当、`GameObjects.cs` の編成売却・使用 | 主に `ViewModel/ViewModelBase.cs:65` | InvalidOperationExceptionなどの文章をそのままMessageBoxへ。操作不可・設備不適合・編成不足などの想定内の拒否を理由コードへ移す。引数誤り・内部不整合の診断例外は区別し、全例外を同一の結果型へ機械的に変換しない。 |
| T10 | `Model/GameInfoLogic.cs:223` `CheckAchievement` | `GamePageViewModel.cs:123,145` など | GameOverExceptionの失敗文を表示。ゲーム終了理由と年・期限などのデータを返す境界が必要。なお `DoNextWeek` のNextWeek呼び出しはtryの外にあり、掲載されたcatchには届かない。通知文字列の分離とは別の例外処理の不具合候補。 |
| T11 | `Model/GameInfo.cs:193`、`Util/ScenerioLoadUtil.cs:480,487`、読込中のモデル設定検証 | `View/MainMenuPage.xaml.cs:54` のcatch | 初期ダイヤ超過、軌間・動力の不正指定などの例外文を読み込みエラー画面へ直結。ファイル・項目・行・路線IDなどの診断情報を持ち、UI側で案内と詳細を組み立てる。システム由来のI/O・数値解析例外も一括で表示される点を含む。 |
| T12 | `Util/LogicUtil.cs:205` `AppendMoneyUnit`、`:217` `ConvJapaneseNumeral` | GamePage、TechnologyDevelop、LineInfo、Construct、Reform、TaihisenChange、Line/KeitoDiagramSetting、Vehicle各ViewModel、InfoViewModel | 10万円単位の数値を円・万・億・兆の表示文字列へ変換。計算と同じUtilファイルにあるが表示責務。`Math.Abs`で符号を落とすので、UI側へ移動するだけでは負値表示の問題は直らない。移動と符号仕様変更は別に検証する。 |
| T13 | `Model/GameEnums.cs:31` `ToName` と各 `Display(Name=...)` | 選択肢、路線/車両情報、技術投資一覧、目標説明 | 難易度、軌間、動力、傾斜、座席、待避線、ダイヤ、路線種別、投資額、目標対象の日本語表示名がモデルに入っている。enum値は本体に残し、表示名対応を表示側へ。座席のComfortLevelAttributeにはゲーム計算値があるため表示属性と一緒に移さない。 |
| T14 | `Model/Line.cs:177` `Caption` | `View/LineInfoPage.xaml:36` `DisplayMemberPath` と `GameInfo.cs:193` の初期化エラー | 路線名と始終点名を「～」で連結する表示用プロパティ。表示側へ移し、エラーの対象識別には路線データを使う。 |

T02～T05の通知追加箇所は合計23か所。これは通知種類のAPI設計数ではなく、現コードの追加箇所数。週次処理の途中で例外になると、それ以前に追加した通知リストは呼び出し元に返らないため、状態変更・通知・終了をどこまで結果として返すかを決める必要がある。

## 表示文字列が意味や同一性に関与する箇所

| 箇所 | 確認した内容 | 分離時の対応 |
| --- | --- | --- |
| `ViewModel/Component/TaihiViewComponent.cs:27` `Equals` | enumに加えてCaptionも比較。 | 同一性をenumで表現し、表示名を変えても同じ選択肢になるようにする。WPFが使うobject.Equalsとの関係は別に確認する。 |
| `ViewModel/Line/ConstructWindowViewModel.cs:212` `LaneNumViewModel.Equals` | CaptionとLaneSuを比較。 | 線数で比較する。 |
| 同ファイル`:232` `RailTypeViewModel.Equals` | Captionに加えて軌道・軌間・電化を比較。 | 規格の構造化した値で比較し、Captionを同一性から外す。表示名を変えても同じ規格として扱う。 |
| `whr_wpfTests/Model/WeeklyCurrentBehaviorTests.cs:193,196,210,230` | 戦時・達成通知の日本語部分一致でイベントを検査。 | 状態・イベント種別とデータを検査し、表示文章のテストは表示層へ移す。 |
| `OriginalStatePortTests.cs:196`、`SelectedBehaviorTests.cs:105`、`TaihisenChangeWindowTests.cs` | 売却見積、再建説明、待避線見積などの表示も検査。 | 表示のテストとして維持できる。純粋なゲーム計算テストとはプロジェクト・分類を分ける。 |

調査範囲の本体で、週次通知や判定理由の日本語を部分一致してゲーム処理を分岐する箇所は見つからなかった。ただしUIは表示名を選択肢の同一性比較に使い、テストは通知文の内容に依存している。「既に本体が通知文を解析している」とは断定しない。

## 直接のUI依存・副作用

| 箇所 | 内容 | 分離案 |
| --- | --- | --- |
| `Model/GameInfo.cs:380` | WPFのBitmapImageを保持。 | 本体はマップ参照情報を保持し、UIで画像を生成する。 |
| `Util/ScenerioLoadUtil.cs:50,55` | 不正な版・基準年をMessageBox表示しForceExit。 | 読み込みエラーとして返し、継続・終了はUIで決める。 |
| 同ファイル`:77` | BitmapImageの生成。 | シナリオ解析・参照情報と画像読み込みを分ける。 |
| `Util/ApplicationUtil.cs:20,31,77,98` | 終了確認・Shutdown・保存読込不可の案内。 | アプリ/UI側に置き、純粋なゲーム本体から参照しない。ファイル読込・保存形式の処理はI/O側として別に扱う。 |
| `Model/Line.cs:944`、`ScenerioLoadUtil.cs:99`、`ApplicationUtil.cs:53` | Consoleへの診断・完了通知。 | 表示文そのものとは区別する。モデルからの直接出力は診断境界へ、読み込み完了のログはI/O側へ。 |

`INotifyPropertyChanged`とプロパティ名、nameofによる引数名は表示文言ではなく通知・診断用。WPF固有の型でもないため、すべて除去する必要はない。

MoneyShortExceptionはViewModelBaseの71～73行、ConstructWindowViewModelの323～325行などで捕捉され、例外のMessageを使わずUI側の固定文「お金が足りません」へ置換される。モデルにも同じ文はあるが、そのまま表示する経路とは区別する。

## 例外文言の確認箇所

調査時の日本語を含む例外生成箇所を、想定内の拒否・データ不正・内部診断も含めて列挙する。これら全部が通常操作で表示されるという意味ではない。任意の例外文を表示する共通catchがあるため、分離作業では表示対象の分類が必要。

| ファイル | 行 | 主な用途 |
| --- | --- | --- |
| `Model/Line.cs` | 169,253,381,457,487,528,573,618,663,672,713,777,804,807,808,809,810,811,814,850,862,874,930,931,961 | 距離、ダイヤ、建設・改造不可、設備不適合、運行本数、編成不足、未定義の計算経路。 |
| `Model/KeitoDiagram.cs` | 248,258,262,275 | 負の本数、未建設・休止区間、ダイヤ不適合、編成不足。 |
| `Model/GameObjects.cs` | 298,348,356,360,368,412,420,426,430,516,528,541,558 | 売却・数量・投入数不足、座標上限、人口・貨物規模の不正。座標の980×684制限は表示寸法との結び付きも確認対象。 |
| `Model/GameInfo.cs` | 193,498 | 初期ダイヤ不正、資金不足。 |
| `Model/GameInfoLogic.cs` | 118,226 | 車両開発不可、目標期限切れ。 |
| `Model/GameInfoWeekly.cs` | 87 | 乗継需要計算の内部エラー。「作者まで報告ください」という案内がロジックに埋め込まれている。 |
| `Model/GameEnums.cs` | 45,56 | 座席計算属性の欠落。 |
| `Util/LogicUtil.cs` | 30,155,192,219 | 座席・路線種別・待避線の不正値、表示整形の負数拒否。 |
| `Util/ScenerioLoadUtil.cs` | 480,487 | シナリオの軌間・動力不正。 |
| `Util/ApplicationUtil.cs` | 68,88 | 保存・旧形式読込の利用不可。 |

これに加えて `CompositionFactory.CreateComposition:356` は `CheckMakeComposition` の文言を例外へ渡す。日本語リテラルを直接含まない `ArgumentNullException`、`ArgumentOutOfRangeException`、I/O・数値解析例外もある。引数名を含む.NET既定メッセージをそのまま利用者へ見せるかは表示側で判断する。

## 本体に残せる文字列データ

- 駅・路線・系統・車両・編成の `Name` はシナリオや利用者が与えるデータ。文字列だからという理由で表示層へ追い出さない。
- `Mode.Message`（`GameObjects.cs:33`）はシナリオ作者の説明文。`ScenerioLoadUtil.cs:355`で区切りを改行へ変換し、`GamePage.xaml.cs:33`が表示する。ロジック生成の判定理由とは別のコンテンツとして扱い、改行変換の担当は検討する。
- `GameInfoWeekly.cs:107`付近は都市名をソートし `"___"` で連結して競合グループのキーにする。UI文言ではないが、名前に区切りが含まれる場合の衝突と改名の影響がある。都市の同一性を表すキーの設計課題として別に追跡する。
- CSVの列、modのプロパティ名、ファイルパス、文字コード指定は解析・I/Oの契約。表示文言の変更とは分ける。

## 移行時に守ること

1. まず発生条件、通知順序と回数、判定の優先順位、成功・失敗時の状態をテストで記録する。今回の棚卸しだけでは新しいテストを追加しない。
2. 週次イベント、検証理由、想定内の操作拒否を構造化する。表示側の変換では、現行の文言をいったん維持できる。
3. 種類だけでなく、技術・対象・変更前後値・不足数・速度上限など、必要なデータを渡す。詳細を文字列に詰め替えて渡すだけでは分離にならない。
4. 表示専用の金額整形・enum名・Captionを本体の参照先から外す。テストから私有フィールドを書き換える乱数制御も、ハーネスの入力へ置き換える。
5. 分離中は数式・費用・割当・期限の仕様変更と混ぜない。既知の不具合は再現テストと別変更で扱う。UI側で確認ダイアログを省いても本体が入力検証する境界にする。

この一覧は静的調査の結果。全画面を実プレイで検証した結果ではない。モデル抽出時は本体のプロジェクト参照を制限し、UI・I/O混入をビルドでも検出する。
