# 共通操作窓口：仕様・影響調査

2026-10-08。Issue #28 / #23、PR #55後続。仕様・影響段階。APIの型・実装は未決。

## 目的と範囲

ADR 0006の主要操作を、WPF・将来のUI・ハーネスが同じ本体から実行できるようにする。既存の公開操作も窓口の候補とし、全メソッドを機械的に包むfacadeは要求しない。UIに残る状態登録や検証を特定し、具体的な不足を操作群ごとに解消する。

ADR 0019の失敗週は、変更済み状態とPropertyChangedを保持しGameEventを返さない。ADR 0024のCore←Scenario、Core/Scenario←WPFの依存、読込順・画像資源寿命を維持する。保存/Web方式、乱数統制(#29)、原子性、既知不具合補正を混ぜない。

## 全操作の対応と影響

パスはリポジトリ相対。行番号は調査時点。操作固有のGameEventは週送りだけが返し、他操作の通知は主にMoney/LaneNumのPropertyChangedとVMの再表示。未知の例外や通知subscriberの例外を想定内拒否へ一括変換しない。

| 操作 | 本体API・製品呼出 | 入力・見積・拒否・成功・途中状態 |
| --- | --- | --- |
| シナリオ開始 | Scenario/ScenerioLoadUtil.LoadFile → View/ScenarioPresentation.LoadScenario → MainMenuPage.NewStart。DifficultyLevelSelectPageがDifficulty設定、ModeSelectPage.NavigateGame:97がSelectedMode設定 | Loaderは版/年→地図callback→駅/路線/経路/系統→Mode。成功GameInfo、解析失敗は診断例外、地図確保後の失敗もある。難易度/Mode所属・再適用は未検証。GameInfo.cs:34 setterは選択を先に変更し、private ApplyModeSettingで年/金/期限→人口/技術→設備/運行→本数→編成→人口初期化。失敗時は途中状態を保持。 |
| 路線建設・再建 | Line.CalcConstructCost/CanConstruct/Construct。唯一の製品呼出はLine/ConstructWindowVM:157/168/313 | 速度40以上・鉄道狭軌300/標準360・リニア990以下、線数1以上、鉄道は電化/軌間入力を本体で検査。拒否LineConstructionFailure→資金検査/支払→設備更新→帳簿→通知。待避線enum/既設への再建/所属は未検証。見積は入力検査をせず計算。UI速度setterも範囲検査。休止再建は保存設備をUIで選び通常建設費を再支払。 |
| 路線改造 | LineのCan/Get/Calc/実行各組。ReformVM:48–134が全改造、TaihisenChangeVM:72/78が待避線。SpeedUpは直接LineDiagramSettingVM:197でも呼ぶ（系統VMの同名処理はコメントアウトされており製品callerではない） | 可否拒否LineEquipmentFailure→費用検査/支払→設備更新。軌間/電化変更は投入編成・関連系統を解除。増設は線数/待避線/ダイヤを変更。速度向上は現在値判定の上限超過候補が既知。待避線はenum未検証。見積→実行間に状態が変わるため実行で再検査する既存契約を維持。 |
| 路線削減・休止 | Line.ReduceOrRemoveLane:754、ReformVM:134 | 未建設拒否。複線以上は1または2線減、単線はIsExist=false/IsSuspended=true、設備保持。どちらも投入編成と関連系統を返却/解除。一般例外が返却途中で起これば原子性保証なし。ADR 0003の設備保持・有料再建を維持。 |
| 技術投資 | GameInfoLogic.cs:15–40 Can各部門。TechnologyDevelopVM:66–176がweeklyInvestmentの5fieldへ直接書換 | 可否はUI表示だけ、setterの拒否/enum定義値検証なし。週次額の合計見積なし。設定は支払なし、週次GameInfoWeekly.cs:355で部門順支払→累計/開発。SpendMoneyではなくAddMoneyで負残高許容。開発イベント/停止は既存週次処理。可否と週次継続を揃えることは仕様変更なので判断待ち。 |
| 車両作成 | VehicleDevelopVM:180/193/198/235→GameInfoLogic.CheckCreateVehicle:55/DevelopVehicle:117、GameInfo.CalcPurchaseVehicleCost/CalcDevelopVehicleCost:522/535 | 本体で名前/技術/速度上限/期限/設備を構造化検査。再検査→支払→vehicles.Add、成功void。VMに速度ボタン0..990、座席変換。負速度/未知enum/技術0速度0は既知候補。見積/検査/実行は同じ入力を維持すべきだが、本体計算式は移管に混ぜて直さない。 |
| 編成作成・登録 | CompositionMakeVM:108/133→CompositionFactory.Check/Create（LogicUtil.cs:271/326）。VM:184のgameInfo.compositions.Addが製品唯一の直接登録 | Factoryは正数量だけ採用、混在/最低速度40を構造化検査、成功Composition返却。登録がUIに残り、画面なしでゲームへ追加する操作が不足。見積VM:95は全数量のprice合計。VM数量ボタン0..16、本体は非正数量を除外。チェック/作成で列挙を繰り返す。入力規則の追加は別の明示判断が必要。 |
| 編成売買 | CompositionManageVM:108/109見積、:158 Purchase/:182 Sale。直接/系統DiagramSettingVM:154/167不足購入。IComposition.CalcSalePrice/Purchase/Sale（GameObjects.cs:260/281/295）。GameInfoLogic.BuyComposition:333は製品未使用 | 購入:負数拒否/0noop→int価格積→支払→余剰加算。売却:gameInfo null検査→余剰比較→long売価→Use内の負数検査→Money加算。Money/Stock理由は既存payloadを利用。成功void。価格overflow/所属未検証は無断修正しない。Money通知例外で購入在庫増加前に止まり得る。 |
| 直接運行 | LineDiagramSettingVM:123/154/159/210–226→Line.ValidateCompositionAcceptable:841/CalcMissingCompositions:915/SettingComposition:938/必要数:1073/ダイヤ見積:1110 | 負本数/null→路線状態→適合→必要数/不足→旧解除→Use→割当。UIはJudgeSuitableDiagram=Noneを拒否するが本体はArgumentException経路。頻度上限/任意diagram未検証。0本数+編成参照は解除とは異なる。明示の直接解除APIなし。余剰二重計算で旧解除後失敗はF06/F20判断待ち。 |
| 系統運行・解除 | KeitoDiagramSettingVM:135/167/171–179/232–249→KeitoDiagram.ValidateCompositionAcceptable:62/JudgeDiagramForRunningPerDay:190/SettingComposition:246/CalcMissingCompositions:308/必要数:333。公開DiagramReset:85 | 負/null→路線建設→頻度判定→路線順適合→必要数/不足→旧解除→Use→割当/line.diagram変更。見積判定はline.diagramや編成/本数を一時変更し復元、例外時復元保証なし。None/未知値の無限ループ候補は記録済み。解除は返却→参照/数量/本数0、路線ダイヤは保持。 |
| 週送り | GameInfoLogic.NextWeek:162、GamePageVM.DoNextWeek:112/DoNextMonth（4回）が全製品caller | 入力なし。有効開始状態を前提。収支/人口/投資/年越し/目標などを順次変更。成功だけ順序付きGameEvent、失敗は変更済み状態と通知を保持しイベント非返却。4週は先行成功週を保持。自動再試行/巻戻しは追加しない。 |
| 目標判定・終了 | private CheckAchievement（GameInfoLogic:225）/CheckWeeklyGoals（GameInfoWeekly:557）。NextWeek以外の製品callerなし | 期限を先に判定→建設/技術/速度/収支/金。達成イベント→Mode目標を消去→期限延長。終了理由はGameOverException.Failure。副作用なし進捗照会APIや終了済フラグなし。private判定をそのまま公開して照会と実行を混同しない。 |

調査はCore/Scenario/WPFの全製品呼出をrgで照合した。GameInfoの公開可変リスト/field、Line/Keitoの投入field、Modeの初期値・目標の一括private化は、解析・初期化・全操作へ波及するため段階を分ける。既存APIの公開互換性と所有モデル所属の追加規則も別途確認する。

## 次段階の候補（未採用）

具体的な不足から着手する候補は、(a)編成の作成＋ゲームへの登録を本体へ移す、(b)部門別投資設定を本体へ移す、の二つ。前者は既存Factoryの検査/返値を再利用してUI登録を解消できる。後者も既存の無検査代入を本体へ移すだけなら可能だが、Canガードの追加や週次継続規則を変える場合は、通常ディーゼル前提など未決仕様へ先に触れる。

まず(a)を既存挙動を保つ責務移動として行えるか、仕様・影響レビューで確認する。これだけで全入力検証や#28の完了とはしない。不要な共通結果型・全操作wrapper・コマンド基底・新プロジェクトは現時点で必要性がない。仕様レビュー後に、対象入力の保持、結果/登録、UI接続、成功/拒否/境界/途中失敗の検証を設計する。

## 判断待ち

投資可否と継続、モード所属/再適用、負の作成入力・未知enum/所有者検査、編成数量16制限、直接運行頻度/解除の意味をpending-decisions.mdへ集約する。F06/F20、通常ディーゼル前提、目標期限など既存判断待ちと重複して新規採用しない。

## 仕様・影響レビュー

Astraの指摘に従い、コメントアウト済みcaller、建設API位置、売却の検査順、投資移管と新しいCanガードの区別を訂正した。編成作成＋登録だけを既存挙動で移す範囲は妥当との評価。設計ではリストの評価順・Factory再列挙と例外・同一結果の一度だけの登録を保持する。

