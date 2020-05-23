﻿# 世界鉄道網(WPFリメイク版)

## 概要

世界鉄道網をC#とWPF(Windows Presentation Foundation)でリメイクしたものです。  
世界鉄道網とは、ポンコツ戦艦山本氏が2000年代にHSP2で開発していた鉄道経営シミュレーションゲームです。  
オリジナル版の世界鉄道網についてはこちら→ <http://hp.vector.co.jp/authors/VA037302/world/index.html>

## 現在のバージョン

0.1.0.0 

## 開発環境

- Windows 10  
- Microsoft Visual Studio Community 2019  
- .NET Core 3.1

### 開発環境セットアップ

開発には以下の環境が必要となります。

1. Microsoft Visual Studio Community 2019をインストール  
    その際にワークロードの「.NET デスクトップ開発」を含めるようしてください。
1. 「whr_wpf.sln」を開き、ビルドしてください。
1. \whr_wpf\bin\Debug\netcoreapp3.1内に、 <http://hp.vector.co.jp/authors/VA037302/world/index.html> からダウンロード、解凍してできるフォルダのうち、jnrフォルダをコピーしてください。
1. Debugで実行すると、アプリケーションが起動します。

### 実行環境セットアップ

EXEを実行する環境には、以下をインストールすることが必要となります。  
開発環境としてセットアップしたものについては、本セクションの手順は不要です。
また、EXEファイルと同じ階層に、開発環境セットアップと同様にjnrフォルダが必要になりますので、上記の手順に沿って取得＆配置してください。

- .NET Core 3.1 Runtime、Desktop Runtime 3.1.4  
   下記ページ内のDesktop Runtime 3.1.4  
   <https://dotnet.microsoft.com/download/dotnet-core/3.1>

## 未実装機能など制約事項

オリジナル版のver1.52比での未実装機能は以下の通りです。  
暇をみて追って開発する予定です。

- ゲーム全般
  - セーブ/ロード  
    (仮で実装しましたが、クラッシュします)
  - チュートリアルモード
  - 任意のシナリオの読み込み  
    読み込めるシナリオはexeと同じ階層のjnrフォルダ内に配置したシナリオのみです。
- シナリオ関連
  - jnrシナリオの敷設済路線の情報の読み込み  
  - 複数のモードの読み込み  
    一番上のモードの設定のみの読み込みに対応
  - ゲーム内目標(路線敷設、技術開発等)
- ゲーム機能
  - 複数週送り
  - 路線毎収支情報の一覧表示
  - 複数マップ切り替え
  - 駅名表示ON/OFF
  - コンテキストメニューからの各種コマンド呼び出し
  - 編成削除
  - 路線運行ダイヤ設定内での輸送力増強
  - 系統ダイヤ設定内でのスピードアップと輸送力増強

## ファイル構成

- whr_wpf.sln → ソリューションファイル
- whr_wpf
  - Model → ゲームロジック関連オブジェクト格納用ディレクトリ
  - Util → 各種ユーティリティー格納用ディレクトリ
  - View → View格納用ディレクトリ
  - ViewModel → ViewModelクラス格納用ディレクトリ
  - App.xaml → アプリケーション開始点
  - App.xaml.cs → アプリケーション開始点コードビハインド
  - AssemblyInfo.cs → アセンブリ情報
  - whr_wpf.csproj → C#プロジェクトファイル

## 設計

### モデル

基本的にはMVVMを採用しています。  
ただし、MVVMに基づいて実装すると難読化する割に保守性が上がらなさそうな箇所や、機能自体が少ない箇所については、コードビハインドにロジックを実装しています。  
また、MVVMに基づいている箇所は、コードビハインド内コンストラクタにてViewModelを初期化する作りとしています。

### 名前空間同士の依存関係について

ViewやViewModelをModelに依存させてはいますが、ModelをView側に依存させない構成にしております。  
Modelは可能な限りUIとは切り離したいです。(やむを得ない箇所は一部ありますが)

## ライセンス

オリジナルのものを継承し、以下に抵触しないようお願いいたします。

1. このゲームを利用して(他人に売る等)お金を稼ぐこと。
2. 無断で不特定多数に対して配布すること。
3. その他日本の法律に抵触すること

## あとがき

C#の経験には乏しく、WPFは初めて使いましたので、学習も目的とした開発となりました。  
読みづらい箇所、設計的に良くない箇所があると思いますが、申し訳ありませんがどうかよろしくお願いします。
