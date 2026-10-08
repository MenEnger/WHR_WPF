# 車両・編成の入力契約

Issue #28 / #23。採用判断は[ADR 0028](../decisions/0028-creation-input-contract.md)。仕様・影響と設計は分けてAstraレビューする。

## Core

VehicleCreationReason末尾へNegativeSpeed/UndefinedPower/UndefinedGauge/UndefinedSeat/UndefinedTiltを追加。CheckCreateVehicleの名前検査直後でチェックし既存recordの入力snapshotで理由を返す。費用関数は既存のまま。

CompositionCreationReason末尾へNegativeQuantity/QuantityExceeded/NegativeVehicleSpeed/UndefinedGauge/UndefinedTrackType/UndefinedPower/UndefinedSeat/UndefinedTilt/UnregisteredVehicleを追加。CompositionCreationCheckへ拒否時だけCarName(string)/RequestedQuantity(int?)/QuantityLimit(int?)/InvalidValue(int?)を追加、負速度は既存ActualSpeedに保存。Car参照を拒否payloadへ残さない。所属拒否の名前も当時値。

CompositionFactory.MaximumVehiclesPerType=16を本体定数とする。internal CheckMakeComposition(name,input,out ImmutableDictionary<Car,int> selected)を追加。public Checkはこれへ委譲、public Createは同じoutから検査後に生成する。名前なしでは入力に触れない。それ以外はinput.ToArrayで元入力を一度完全列挙、全負数量→全16超→正数量のみToImmutableDictionary→空→属性カテゴリ順→既存混在/速度。数量0のCarには触れない。選択null/重複と列挙例外はそのまま伝播。

生成処理はinternal CreateValidatedComposition(name,selected)へ切り出し、選択辞書から既存計算・登録属性を構築。呼出元はFactory.CreateとGameInfo.Createの2箇所だけ、両方check成功が前提。全Carの深いコピーや汎用operation基底は作らない。

GameInfo.CheckCreateComposition(name,input)を追加。private PrepareComposition(name,input,out selected)でFactoryの検査後にselected.Keysとvehiclesの参照同一性を確認。public Createはdestination=compositionsを先に確保→Prepare→拒否なら専用例外→CreateValidatedComposition→destination.Add。一度の入力列挙で検査と生成を一致させる。CheckとCreateは別操作で各回再検査。

## WPF

CompositionMakeVM ErrorMsg/CanExecuteはGameInfo.CheckCreateCompositionへ変更。QuantUpの上限を本体定数へ一致。Descriptionの動力未定義表示はToNameを呼ばず番号と「未定義」を示す。編成全体の見積だけは仮の非空名で本体検査し、成功時だけ速度/価格を計算する（名前入力前の車両説明は維持）。NoVehicles時は従来の速度0/価格0を表示する。その他拒否では案内を表示し編成見積を計算しない。

両VMの確定Executeで本体checkを再評価し、不可ならMsgまたはErrorMsgを再通知してreturn（確認/費用計算前）。承認後の既存callbackは本体実行の再検査を利用。既存技術拒否の汎用実行案内は維持、新入力拒否の例外だけ個別Format(check)で表示。

## 検証

変更前VehicleValidation/CompositionRegistration計40件を実行し成功。既存受理試験を採用契約へ更新し、負/0/16/17、属性5enumの999とSeat0、game所属とコピー、複数違反優先、拒否不変/通知0、照会後変更、1回しか列挙しないinputと2回目別入力、初期17両保持、全体16超正常、合法全種類は既存試験と少数の代表ケースで守る。

WPFではformatterの追加理由を検査し、VMのgetter・CanExecute・不正Execute・確認後callbackを実際に呼ぶ接続試験を用意する。拒否表示、見積抑止、未選択の不正車両の説明、所属変更後の再検査を確認する。CloudでCore・Scenarioの全試験とWPFクロスビルドを行う。WPF試験の実行は未確認として記録し、WindowsCIは導入しない。

## 仕様・影響レビューで確定した契約

名前なしは最優先。車両は負速度→動力→軌間→座席→傾斜→既存技術条件。編成は全列挙→負数量→16超→正数量辞書化→空編成→負速度→軌間→軌道→動力→座席→傾斜→既存混在/速度→ゲーム所属。属性ごとに全選択Carを検査する。数量0は属性・所属を検査しない。同名・同属性の別Carは登録済み扱いにしない。低水準計算関数の前提は呼出元が保証し、作成とUI見積に同じ本体検査を使う。

## 実装案の検証と設計相談

2026-10-08、現行メソッド方式の実装案でCore339件・Scenario84件の計423件が成功（失敗・skip0）。WPF試験プロジェクトのクロスビルドは警告・エラー0、WPF試験の実行は未確認。ユーザーから本体Command方式の相談があり、入力型＋GameInfoの型別検査・実行案と既存方式の比較をAstraで調査した。ユーザーは既存方式で進め、Command型はIssue #60で別途検討することを選択した。今回の入力契約PRにCommand型は追加しない。

## レビュー結果

仕様・影響と設計を別々にAstraが確認し、指摘を反映した。単一列挙、検査と生成の一致、ゲーム所属、UIの不正Executeと見積抑止を受入条件に含めた。入力契約部分の仕上げレビューでは差分・網羅性・可読性・保守性・責務・過剰さに修正必須指摘なし。Command案の別Issueへの分離と、既存方式での最終差分を再確認する。
