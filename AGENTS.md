# AGENTS.md

AI コーディングエージェント (および人間のコントリビューター) 向けの作業ルールです。
このリポジトリで作業する前に、最後まで読んでください。

## このリポジトリの目的

Classic ASP 経験者が「これなら読める」と思える、ASP.NET Core Razor Pages の**教材用サンプル**です。
技術ブログの記事から参照されます。したがって、優先順位は次のとおりです。

1. **読みやすさ** ― ASP を知っている人が、上から順に読んで理解できること
2. **正しさと安全性** ― サンプルを真似した人が脆弱なコードを書かないこと
3. 機能の豊富さ、抽象化、性能は**優先しない**

「より良い設計」に見える変更でも、読者が追うファイルや概念が増えるなら採用しません。

## 構成

| パス | 内容 | 変更してよいか |
|---|---|---|
| `apps/web/` | Razor Pages 版のアプリ本体 | ○ |
| `tests/WebApp.Tests/` | 自動テスト (xUnit) | ○ |
| `docs/` | 解説 (`razor-pages-primer.md`) と早見表 (`asp-to-razor-cheatsheet.md`) | ○ |
| `legacy/asp/` | 当時風の Classic ASP 版。**読み比べ用で、実行もビルドもしない** | △ (下記「legacy の扱い」) |
| `docker-compose.yml`、`apps/web/Dockerfile` | 実行環境 | ○ |
| `Directory.Build.props`、`Directory.Packages.props`、`global.json` | 全プロジェクト共通の設定 | 必要なときだけ |

## コマンド

すべてリポジトリのルートで実行します。.NET SDK 10.0 と Docker が必要です。

```bash
docker compose up -d db          # PostgreSQL を起動 (テストと dotnet run に必要)
dotnet build                     # ビルド。警告はエラー扱い
dotnet test                      # テスト。PostgreSQL に実際に接続する
dotnet watch --project apps/web  # 開発用に起動 (http://localhost:5121)
docker compose up --build        # コンテナで全体を起動 (http://localhost:8080)
```

- テストはテスト専用データベース `aspdemo_test` を自動作成します。接続先は環境変数 `TEST_CONNECTION_STRING` で変更できます。
- DB に接続できない環境ではテストを実行できません。その場合は「未実行」と正直に報告し、成功したことにしないでください。

## 完了の条件

変更を「できた」と報告する前に、次をすべて確認してください。

- [ ] `dotnet build` が警告 0・エラー 0 で通る
- [ ] `dotnet test` が全件成功する
- [ ] 画面を変えた場合、実際にブラウザー (または HTTP) で開いて確認した
- [ ] 動きを変えた場合、テストを追加または更新した
- [ ] 下記「ドキュメントとの同期」に該当する場合、文書も直した

## コーディング規約

### 全般

- 画面の文言、コメント、ドキュメント、コミットメッセージは**日本語**で書く。識別子 (クラス名・変数名) は英語。
- コメントは「なぜそうするか」と「ASP ではどう書いていたか」を書く。対応する ASP の書き方がある場所には `// ASP: Request.Form("x")` の形で添える。
- 読者は C# の初心者を想定する。凝った言語機能、独自の基底クラス、リフレクション、ソースジェネレーターは使わない。
- NuGet パッケージは安易に増やさない。追加するときは `Directory.Packages.props` にバージョンを書き、README の「使っている技術」に理由を添える。
- JavaScript / CSS のフレームワークは入れない。スタイルは `apps/web/wwwroot/css/site.css` の 1 枚だけ。

### Razor Pages

- ページの書き方は 2 種類を意図的に使い分けている。勝手に統一しない。
  - **1 ファイル完結スタイル** (`@functions`): `Pages/Index.cshtml`、`Pages/Todo.cshtml`。素の HTML (`name="..."`) を中心に書く。
  - **PageModel 分離スタイル** (`.cshtml` + `.cshtml.cs`): `Pages/Employees/`。タグヘルパー (`asp-for` など) を使う。
- 状態を変える操作 (登録・更新・削除) は必ず **POST** にする。GET のリンクで削除しない。
- POST が成功したら必ず `RedirectToPage(...)` する (二重送信の防止)。結果のメッセージは `TempData["Message"]` で渡す。
- 入力エラーのときは `return Page();` で同じ画面を出し直す。その前に、一覧や選択肢などの表示用データを読み直すこと。
- フォームや URL から受け取るプロパティで、省略できるものは必ず null 許容 (`string?`、`int?`) にする。null 不可の `string` は自動的に必須入力として扱われる。
- 1 つのタグで `asp-all-route-data` と `asp-route-xxx` を併用しない (`asp-route-xxx` 側が URL に出力されない)。追加の値は hidden 項目か、辞書に足して渡す。
- 偽造防止トークンの検証を無効にしない (`[IgnoreAntiforgeryToken]` を足さない)。
- `@Html.Raw` を使わない。どうしても必要な場合は、値が利用者の入力に由来しないことをコメントで説明する。
- JavaScript の中に `@値` を直接埋め込まない。`data-*` 属性に入れて `this.dataset` で読む (`Employees/Index.cshtml` の削除確認を参照)。

### データベース

- SQL は手書きし、Dapper で実行する。Entity Framework Core などの O/R マッパーは導入しない (ADO の感覚を残すため)。
- **値は必ず `@パラメータ` で渡す。SQL を文字列連結や `$"..."` で組み立てて値を埋め込まない。**
  定数の SQL 断片同士を連結するのは構わない (`EmployeeRepository` の `SelectColumns` など)。
- `LIKE` 検索の入力は `EmployeeRepository.ToLikePattern` を通し、`%` と `_` をエスケープする。
- テーブル定義は `apps/web/Data/schema.sql` だけに書く。起動のたびに実行されるので、必ず**何度流しても同じ結果になる**書き方 (`IF NOT EXISTS` など) にする。
- 列名は `snake_case`、C# のプロパティは `PascalCase`。対応付けは Dapper が自動で行う (`Program.cs` で設定済み)。
- 日付だけの値は `date` 型 + `DateOnly`、日時は `timestamptz` + `DateTime` (UTC) を使い、表示するときに `ToLocalTime()` する。

### 文字コード

- ソース、HTML、DB はすべて UTF-8。
- Shift_JIS を使うのは Excel 向けの CSV 出力だけ (`Csv.cs`)。それ以外の場所に Shift_JIS を持ち込まない。

## ドキュメントとの同期

このリポジトリでは、コードと解説が食い違うことがいちばんの不具合です。次の変更をしたら、同じコミットで文書も直してください。

| 変更したもの | あわせて直すもの |
|---|---|
| ページの追加・削除・URL の変更 | `README.md` の表、`docs/razor-pages-primer.md` の 5.1 |
| `Program.cs` の設定 | `docs/razor-pages-primer.md` の 9 (つまずきやすい点)、`README.md` の「設定」 |
| ASP との対応が変わる書き換え | `docs/asp-to-razor-cheatsheet.md` の「実例」列、`legacy/asp/README.md` の対応表 |
| パッケージやランタイムのバージョン | `README.md` の「使っている技術」、`Dockerfile`、`global.json`、`.github/workflows/ci.yml` |
| 画面の見た目 | `docs/images/` のスクリーンショット |

ドキュメントに事実 (製品のサポート期限、バージョン、日付など) を書くときは、一次情報を確認して出典のリンクを付けます。確認できないことは書きません。

## legacy の扱い

- `legacy/asp/` は「当時ありがちだった書き方」をわざと残した資料です。SQL の文字列連結や GET での削除は**意図的**なので、修正しないでください。
- 触ってよいのは、`apps/web` 側の機能を変えたときに、読み比べが成り立つよう対応する `.asp` を合わせる場合だけです。
- 実行環境 (Windows + IIS) が無いので動作確認はできません。`legacy/asp/README.md` の「実行確認をしていない」という記述を消さないでください。
- `legacy/asp/` のコードを `apps/web` に持ち込まない。BASP21 などの COM コンポーネントへの依存も追加しない。

## やってはいけないこと

- 本物らしく見える個人情報を初期データに入れる (氏名は架空、メールは `example.co.jp`、電話番号は使わない)。
- パスワード、API キー、接続先の本番情報をコミットする。サンプル用の `app` / `app` 以外の資格情報は書かない。
- テストを通すために、検証を弱める・テストを消す・スキップにする。
- 依頼されていない大規模なリファクタリング、フォルダー構成の変更、フレームワークの入れ替え。

## コミットとプルリクエスト

- コミットメッセージは日本語で、1 行目に「何をしたか」を書く (例: `従業員一覧に部署での絞り込みを追加`)。
- 1 つのプルリクエストでは 1 つのことだけを行う。
- プルリクエストの説明には、変更の理由、確認した方法 (実行したコマンドと結果)、画面を変えた場合はスクリーンショットを書く。
