# legacy/asp ― 当時風の Classic ASP 版 (読み比べ用)

2000 年代の社内システムでよく見かけた書き方を再現した Classic ASP (VBScript + ADO) のコードです。
`apps/web` の Razor Pages 版と **同じ画面・同じ機能** になるように書いてあり、並べて読むための資料です。

> [!IMPORTANT]
> このフォルダーのコードは **Docker / Linux では動きません。また、作者の環境でも実行確認をしていません。**
> Classic ASP は Windows の IIS 専用で、VBScript の実行エンジンも Windows にしかないためです。
> ビルドにもテストにも含まれていません。

## ファイルの対応

| Classic ASP (このフォルダー) | Razor Pages (`apps/web`) | 内容 |
|---|---|---|
| `global.asa` | `Program.cs` | 起動時の処理、アクセスカウンターの初期化 |
| `inc/conn.asp` | `Program.cs` の `NpgsqlDataSource` 登録、`Data/EmployeeRepository.cs` | DB 接続と共通関数 |
| `inc/employee_where.asp` | `Data/EmployeeRepository.cs` の `WhereClause` | 検索条件の組み立て |
| `inc/header.asp` / `inc/footer.asp` | `Pages/Shared/_Layout.cshtml` | 全画面共通の枠 |
| `default.asp` | `Pages/Index.cshtml` | メニュー、担当者名 (Session)、カウンター (Application) |
| `todo.asp` | `Pages/Todo.cshtml` | ToDo |
| `employees.asp` | `Pages/Employees/Index.cshtml` + `.cs` | 従業員一覧・検索・ページ送り・削除 |
| `employee_edit.asp` | `Pages/Employees/Edit.cshtml` + `.cs` | 従業員の登録・修正 |
| `employees_csv.asp` | `Index.cshtml.cs` の `OnGetCsvAsync` | Shift_JIS の CSV 出力 |

## わざと「当時のまま」にしてある点

読み比べのために、当時ありがちだった問題をあえて残しています。**新規のコードで真似しないでください。**

- **SQL を文字列連結で組み立てている。** `'` を `''` に置き換える自前の関数 (`SqlStr`) だけが頼りで、
  1 か所でも通し忘れると SQL インジェクションになります。Razor 版はすべて `@パラメータ` です。
- **HTML エスケープが手動。** `H()` (`Server.HTMLEncode`) を書き忘れた場所がそのまま XSS になります。
  Razor 版は `@値` と書くだけで自動的にエスケープされます。
- **CSRF 対策が無い。** 削除や更新のフォームに偽造防止トークンがありません。
  Razor 版は `<form method="post">` に自動でトークンが入り、検証されます。
- **削除が GET リンク。** `employees.asp?del=5` を踏むだけで消えます (クローラーや先読みでも消える)。
- **接続文字列がソースに直書き。**

## 実機で試したい場合

- Windows の IIS に「ASP」機能を追加し、このフォルダーを仮想ディレクトリにします。
- スタイルシートは `apps/web/wwwroot/css/site.css` を同じフォルダーにコピーして使います。
- DB は SQL Server を想定しています。テーブル定義は `schema.sqlserver.sql` を参照してください。
- ファイルは UTF-8 (BOM なし) で保存してあり、各ページ先頭で `CodePage=65001` を指定しています。
  当時は Shift_JIS 保存 + `CodePage=932` が一般的でした。
- VBScript は Windows で非推奨になっており、将来の Windows では既定で無効になる予定です。
  詳しくは [`docs/razor-pages-primer.md`](../../docs/razor-pages-primer.md) を参照してください。
