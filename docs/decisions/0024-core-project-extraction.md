# ADR 0024: ゲーム本体と旧形式解析のプロジェクト抽出

日付: 2026-10-08
状態: 採用。Issue #28の第一段階。

## 背景

ADR 0006のUIを交換できる構成に向け、モデルは表示依存を整理済みだがWPFプロジェクトに残っている。本体のみのビルド・試験をプロジェクト参照で保証する必要がある。共通操作APIと抽出を同時に変更すると、状態や途中失敗の変更を見分けにくい。

## 決定

同じソリューションにnet9.0のwhr_core、whr_scenarioを追加する。モデル17ファイルとLogicUtilを本体へ、旧形式解析と解析例外をシナリオへ移す。シナリオは本体だけを参照し、本体はUIと解析を参照しない。WPFは両者を利用し、画像・表示・ログ・資源の配置と寿命を維持する。namespace、数式、解析順序、公開操作とADR 0019の途中失敗契約は変更しない。

内部LogicUtilの既存利用には、世界鉄道網とwhr_scenarioだけにInternalsVisibleToを付与する。全internalへのアクセスが許されるため、追加のfriendや公開API化は行わない。操作窓口の設計時にこの利用境界を再検討する。

主要4試験ファイル107件をnet9.0のwhr_coreTestsへ移す。モデルfixtureは単一ソースをリンクし、テストassembly間の参照や複製を作らない。残る純粋試験と解析試験の移管は、共有fixtureと混在試験の整理を後続で行う。

## 代替案

- 全てをWPFへ残す：本体だけで動くことをビルドで保証できない。
- 解析を本体へ含める：ゲームルールと旧ファイル形式の依存を分ける目的に合わない。
- 全試験移管と共通操作APIを一度に追加する：今回の境界抽出に必要な範囲を越え、差分の追跡と互換性確認が難しくなる。
- 内部計算を公開化する：抽出だけのために公開契約を広げる必要はない。

## 影響と検証

namespaceは同じでも公開型のassembly identityは変わる。外部binary、assembly-qualified名、reflection、Serializableの互換性には影響する。現在は実装された保存serializerがないため、型forwarderや新しい保存移行は追加しない。UI内部型をモデルassemblyから探す3試験はUI型を基点に変更する。

移動前後の107件のテスト名とケース数、残る426件と合わせた533件、本体単独ビルド、FrameworkReference/ProjectReference、WPF配布出力のDLLとjnr資源を確認する。Windows上での確認をLinux上の実行済み証拠としては扱わない。

仕様・影響レビューと設計レビューを別々にAstraが承認した。詳細は[抽出設計](../design/core-project-extraction.md)。Issue #28の共通操作窓口と全体の完了条件はまだ残る。
## 第二段階の試験配置

[本体試験の追加移管](../design/core-test-separation.md)では、独立した10ファイル135件とCurrentBehaviorFixtureを本体試験へ移す。fixtureは本体のみを利用するため本体試験側を正本とし、WPF試験から単一ソースをリンクする。第一段階の逆向きソースリンクを解消し、複製や共有試験ライブラリを増やさない。製品・試験の期待値とassembly間ProjectReferenceは変更しない。イベント期待値を共有する試験、混在試験とシナリオ単独検証は後続に残る。

## 週次通知試験の共有データ

[週次通知試験の分離](../design/weekly-notification-test-separation.md)では、表示試験が通知試験クラスから利用していたイベント期待値と状態生成処理をCoreTestsのWeeklyEventFixtureへ置く。WPFは単一ソースをリンクし、通知試験本体はCoreへ移す。表示文の期待値はWPFへ残す。試験クラス同士のassembly参照や同じデータの複製を避け、既存のイベントと表示の対応順・途中失敗契約を維持する。新しい共有ライブラリやimmutable APIは今回必要ないため追加しない。

## 解析単独の試験構成

[シナリオ試験の配置](../design/scenario-test-separation.md)でnet9.0のwhr_scenarioTestsを追加し、Core/Scenarioだけを参照する既存19試験を実行する。既存Corefixtureとシナリオ生成fixtureを単一ソースで共有する。共有のScenarioFilesは解析試験側の独立internal型とし、WPFからソースリンクする。製品の解析順序とUI資源寿命を変えず、解析単独の依存境界を確認するためにこの試験プロジェクトを採用する。共有試験ライブラリや解析/表示の混在試験分割は本段階に追加しない。
