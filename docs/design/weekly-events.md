# 週次通知の構造化

2026-10-07。Issue #26の第一段階。週次通知23箇所を対象にする。

## 仕様検討

週送りは表示文章ではなく、動力の完成・速度改良・費用低下、新企画技術の完成、蒸気の設定終了、戦時開始/終了、目標達成をデータで返す。動力・技術の種別と、発生時点の変更前後の技術値、運行容量、戦時期間/貨物係数、実行中の期限/延長期限を保持する。返した通知は後の週送りやモデル変更で内容が変わらない。

UI側で現在の日本語文と改行を生成する。発生条件・順序・回数、数式・投資停止・年次処理・達成後の状態を維持する。同じ週に速度改良と費用低下が起きる場合は、それぞれの時点の値を保持する。週単位と4週送りの通知順と区切りも維持する。

失敗時の例外型・期限優先順・途中状態は維持する。例外発生前の通知リストは呼出元へ返らない現状を変えない。週送りのtry範囲の不具合、ゲームオーバー文言、操作拒否、車両・編成の判定理由、乱数制御は今回変更しない。Issue #26・#28は未完了としてRefsで追跡する。

## 影響確認

- 製品でNextWeekの戻り値を使うのはGamePageViewModelの一週/四週送り。その他はテスト。NextYearと週次のprivate helperも通知リストを使う。
- 公開NextWeekの戻り値を変更するため、外部でList<string>と文字列処理をしていた利用側は移行が必要となる。モデル側に表示互換wrapperを残さない。
- WeeklyCurrentBehaviorTestsとGoalDeadlineRegressionTestsの日本語部分一致を種別/データ検査へ移し、表示文の検査はUI側へ分ける。その他の収支・人口・状態の既存テストは維持する。
- 目標失敗はGameOverExceptionのまま。週送り前段の開発・支出・年越し・人口更新を巻き戻さず、例外時に新たな通知結果を返さない。一週送りのcatch位置も今回は動かさない。
- 新しいイベントの型はモデル内に置き、WPF・UIformatter・表示名・ファイルI/Oを参照しない。戦時設定やGameInfoそのものの可変参照を通知へ保持しない。

仕様・影響レビュー（Astra、同日）で致命的不足なし。四週の途中失敗では先行週の通知も表示されない現状を維持する。動力イベントの種別は既存分岐に対応させ、技術値から再分類しない。現行ディーゼルは360以上でも速度改良分岐に入るため、技術値を一律に実速度と解釈しない。

## 設計

モデルにimmutableなrecordを追加する。`GameEvent` を基底とし、次を区別する。

- `EngineDevelopedEvent(PowerEnum Power, EngineDevelopmentKind Kind, int PreviousLevel, int CurrentLevel)`。KindはAvailable/SpeedImproved/CostReduced。実行した分岐の種別と、その分岐直前・直後の技術値を記録する。
- `SpecialTechnologyDevelopedEvent(SpecialTechnology Technology, int PreviousCapacity, int CurrentCapacity)`。Technologyは既存10企画に対応するenum。容量の変更がない企画は前後値が同じとなる。
- `SteamAvailabilityEndedEvent(int Year)`。
- `WarStartedEvent(int StartYear, int EndYear, int FreightIndex)`、`WarEndedEvent` は同じ引数。設定解除前の値をコピーする。
- `GoalsAchievedEvent(int PreviousDeadline, int FreeModeDeadline)`。実行中期限とBasicYear+420の実際の延長値を記録する。

NextWeek/NextYearと通知helperをList<GameEvent>へ変更する。既存の文章追加位置をイベント追加へ置換し、判定・状態更新の順を変えない。同じ動力の速度/費用が連続する場合も、それぞれの直前値を保持する。特殊技術の容量イベントは既存追加位置で予定される後値を記録し、実際の状態更新は旧位置で行う。

View側の `WeeklyEventFormatter.Format(GameEvent)` と `FormatMany(IEnumerable<GameEvent>)` が現在の23文章と区切りを生成する。GamePageViewModelはリストの型と表示変換だけを変更する。一週のtry範囲、四週のループ・通知タイミング・catchは維持する。実ダイアログを操作するテストは今回追加せず、成功時の複数週集約と途中失敗で返却結果がないことをモデル呼出で確認し、UI接続は差分で照合する。

実装前の2ケースで、19通知が同時に起きる順序・中間速度360と最終値370、期限失敗時に返却されない通知と変更済み状態を確認済み。採用判断はADR 0011へ記録する。

正式設計レビュー（Astra、同日）で実装開始可。派生recordをsealedとし、未知の通知をformatterが黙って消さないこと、先行技術の容量変更を後続通知にも反映することを確認した。容量・戦時解除・期限更新の値の時点と、実ダイアログの未検証範囲を検証記録へ明記する。

## 検証計画

実装前に、動力→新企画→蒸気終了→戦時→目標の通知順、同一動力の連続改良、年次と期限失敗の途中状態を現状テストで記録する。移動後は全通知種別、変更前後値、複数同週・年越し・一度だけ発生する条件・後続週による通知の不変性を検査する。UI側では既存23文章と4週の区切りを確認する。

検証結果：変更前の2ケース成功。移動後は対象100件、全体321件が成功した。Astraのコード・網羅レビューでは製品の修正必須指摘なし。テスト側の誤期待2点を解消し、条件・数式・UIのtry/ループ位置を変更せず仕上げた。
