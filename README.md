# しゃどレコ！

## 概要

**しゃどレコ！** は、「Shadowverse: Worlds Beyond」の試合戦績を自動記録する非公式ソフトウェアです。  
試合開始・進行中・結果の画面を検出し、使用クラスや勝敗を収集・集計します。  
記録結果は UI 上で履歴・クラス別統計として閲覧可能です。  

> ⚠️ 本ソフトウェアは **Cygames, Inc. 非公式のファンメイドツール** です。Cygames とは一切関係がありません。  
> ⚠️ 本リポジトリには Shadowverse の画像等の著作物は含まれていません。利用時にテンプレート画像バケットから利用者の環境に自動取得されます。  
> ⚠️ 取得される画像の著作権は Cygames, Inc. に帰属します。商用利用・再配布は禁止です。  

---

## 主な機能

* ゲーム画面をキャプチャしてリアルタイムに試合状況を検出
* 自分/相手のクラス、試合形式、勝敗、先行後攻を自動記録
* 履歴タブによる時系列一覧表示
* 勝敗タブによる対面ごとの勝率分析

---

## インストールと利用方法

### 動作環境

* Windows 10/11 推奨
* .NET 8 ランタイム

### 利用手順

1. [リリース](https://github.com/gryyyyymnyl/DuelLedger/releases/tag/v1.0.0)からバイナリをダウンロードし、任意のフォルダに展開
2. `DuelLedger.UI.exe` を起動
3. ゲームをプレイすると、試合結果が自動で記録されます
4. 記録は `out/` ディレクトリ以下に JSON 形式で保存されます
5. UI から履歴や統計を確認可能です

---

## 開発者向け情報

### ビルド手順

1. リポジトリをクローン

   ```bash
   git clone https://github.com/gryyyyymnyl/DuelLedger.git
   cd DuelLedger
   ```
2. .NET 8 SDK をインストール
3. 必要パッケージを復元

   ```bash
   dotnet restore
   ```
4. UI プロジェクトをビルド

   ```bash
   dotnet build ./UI/DuelLedger.UI.csproj -c Release
   ```
5. 実行

   ```bash
   dotnet run -c Debug -f net8.0-windows --project ./UI/DuelLedger.UI.csproj
   ```

### プロジェクト構成

共有のDTOや列挙体は `Core/Abstractions` プロジェクトに分離されています。UIや検知ロジックから参照する際はこのプロジェクトを通じて利用します。
Shadowverse 固有の検知やテンプレート定義は `Games/Shadowverse` プロジェクトにまとめられています。

アプリ設定は `Infra/Config` の `AppConfigProvider` が `appsettings.json` と `remote.json` をマージして提供します。リモート取得に失敗してもローカル設定で起動します。

検知処理は `Core/Pipelines` の `MatchPipeline` が `IFrameSource` → `IDetector` → `SnapshotAggregator` → `ISnapshotPublisher` の流れで実行し、Format 検知が失敗しても直前の値を保持します。

時間取得は `Core/Util/SystemClock` を介した `IClock` 抽象で行われ、テストでは差し替え可能です。

スナップショットと試合結果は PascalCase の JSON (`Format`, `SelfClass` など) として `out/current.json` へ原子的に書き出され、UI は `FileShare.ReadWrite | FileShare.Delete` で監視します。リネーム/作成/変更イベントで即座に状態が反映されます。
UI のクラスアイコンは `SvgIconCache` が非同期に取得し、キャッシュ済みでない場合はテキストのみを表示して後から更新されます。
ファイル出力などの外部 I/O は `Util/Retry` による指数バックオフリトライで最大3回再試行され、失敗しても処理が停止しないようになっています。

### コンソール実行 (Runner)

UI なしで動作させたい場合は Runner を使用します。

```bash
dotnet run --project ./Runner/DuelLedger.Runner.csproj
```

## 注意事項

* 本ソフトウェアは非公式ツールであり、Cygames公式のサポート対象外です。
* 本ソフトは画像認識による「観察」のみを行い、ゲーム内部データを改ざん・ハッキングしません。
* 記録はローカル保存のみで、外部サーバーへの送信は行いません。
* Shadowverse の画像・アイコン等はリポジトリには含まれず、利用者環境でテンプレート画像バケットから取得されます。
* 自動取得される画像の権利は Cygames, Inc. に帰属します。利用は私的・個人の範囲にとどめてください。

---

## ライセンス

* **本リポジトリのソースコード** は [MIT License](./LICENSE) の下で公開されています。
* **Shadowverse の画像・アイコン等** は本ライセンスの対象外です。著作権は Cygames, Inc. に帰属します。
* 利用者は画像の商用利用・再配布を行わないでください。

---

## 貢献

* バグ報告や改善要望は [Issues](https://github.com/gryyyyymnyl/DuelLedger/issues) へ
* 新機能の提案やプルリクエストは歓迎します

---

## 今後の計画
* 戦績データの CSV / JSON エクスポート
* 透過モード UI 対応
* 対戦相手別・期間別統計の拡充
* 他カードゲームへの拡張可能性
