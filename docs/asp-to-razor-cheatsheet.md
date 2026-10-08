# Classic ASP → Razor Pages 書き換え早見表

「ASP ではこう書いていたものを、Razor Pages ではどう書くか」の対応表です。
右端の列は、このリポジトリで実際に使っている場所です。
用語の説明は [razor-pages-primer.md](razor-pages-primer.md) を参照してください。

## ファイルと構成

| Classic ASP | Razor Pages | このリポジトリでの実例 |
|---|---|---|
| `todo.asp` | `Pages/Todo.cshtml` (1 行目に `@page`) | `apps/web/Pages/Todo.cshtml` |
| `<%@ Language=VBScript %>` | 不要 (言語は C# 固定) | |
| `<!--#include file="header.asp"-->` | `Pages/Shared/_Layout.cshtml` + `@RenderBody()` | `Pages/Shared/_Layout.cshtml` |
| `<!--#include file="inc/func.asp"-->` | 普通の C# クラス (`.cs`) | `Csv.cs`、`Data/EmployeeRepository.cs` |
| `global.asa` | `Program.cs` | `apps/web/Program.cs` |
| IIS マネージャーでの設定 | `Program.cs` と `appsettings.json` | |
| `web.config` / レジストリの接続文字列 | `appsettings.json` または環境変数 | `docker-compose.yml` の `ConnectionStrings__Default` |

## 出力と制御構文

| Classic ASP | Razor Pages |
|---|---|
| `<%= Server.HTMLEncode(x) %>` | `@x` (自動エスケープ) |
| `<%= x %>` (エスケープなし) | `@Html.Raw(x)` |
| `Response.Write "..."` | HTML をそのまま書く |
| `<% If a Then %> … <% Else %> … <% End If %>` | `@if (a) { … } else { … }` |
| `<% For i = 1 To n %> … <% Next %>` | `@for (var i = 1; i <= n; i++) { … }` |
| `<% Do Until rs.EOF %> … <% rs.MoveNext : Loop %>` | `@foreach (var row in rows) { … }` |
| `<% Select Case x %>` | `@switch (x) { case …: … break; }` |
| `<% ' コメント %>` | `@* コメント *@` |
| `Server.URLEncode(x)` | `asp-route-xxx="@x"` (自動)、または `Uri.EscapeDataString(x)` |
| `FormatNumber(n, 0)` | `@n.ToString("N0")` |
| `FormatDateTime(d)` / 自作の日付整形関数 | `@d.ToString("yyyy/MM/dd")` |

## リクエストとレスポンス

| Classic ASP | Razor Pages | 実例 |
|---|---|---|
| `Request.Form("name")` | `[BindProperty] public string? Name { get; set; }` | `Todo.cshtml` の `NewTitle` |
| `Request.QueryString("show")` | `[BindProperty(SupportsGet = true)]`、またはハンドラーの引数 | `Todo.cshtml` の `Show`、`OnPostDeleteAsync(int id)` |
| `employee_edit.asp?id=5` | `@page "{id:int?}"` → `/Employees/Edit/5` | `Employees/Edit.cshtml` |
| `If Request.ServerVariables("REQUEST_METHOD") = "POST"` | `OnGet` / `OnPost` に分けて書く | 全ページ |
| `Select Case Request.Form("action")` | `asp-page-handler="Add"` → `OnPostAddAsync` | `Todo.cshtml` |
| `Response.Redirect "todo.asp"` | `return RedirectToPage();` | 全ページ |
| `Response.Redirect "employees.asp?q=..."` | `return RedirectToPage("Index", new { q = ... });` | `Employees/Index.cshtml.cs` |
| `Response.Status = "404 Not Found" : Response.End` | `return NotFound();` | `Employees/Edit.cshtml.cs` |
| `Response.ContentType` + `Response.BinaryWrite` | `return File(bytes, "text/csv", "名前.csv");` | `Employees/Index.cshtml.cs` の `OnGetCsvAsync` |
| `Response.AddHeader "Content-Disposition", ...` | `File(...)` の第 3 引数 (ファイル名) | 同上 |
| `Request.Cookies("x")` / `Response.Cookies("x")` | `Request.Cookies["x"]` / `Response.Cookies.Append("x", v)` | |
| `Request.ServerVariables("REMOTE_ADDR")` | `HttpContext.Connection.RemoteIpAddress` | |

## 状態の保持

| Classic ASP | Razor Pages | 実例 |
|---|---|---|
| `Session("User") = name` | `HttpContext.Session.SetString("User", name)` | `Index.cshtml` |
| `name = Session("User")` | `HttpContext.Session.GetString("User")` | `_Layout.cshtml` |
| `Session.Timeout = 20` | `AddSession(o => o.IdleTimeout = TimeSpan.FromMinutes(20))` | `Program.cs` |
| `Session.Abandon` | `HttpContext.Session.Clear()` | |
| `Application("Counter")` + `Application.Lock` | シングルトンのクラスを登録して `@inject` | `AppState.cs` の `AccessCounter` |
| `Session("Message")` を表示後に空にする | `TempData["Message"]` (1 回読むと消える) | `_Layout.cshtml` |
| `Application_OnStart` | `Program.cs` の `app.Run()` より前に書く | `DatabaseInitializer` の呼び出し |

## データベース (ADO → Dapper)

このリポジトリでは、SQL を手書きする感覚を残すために [Dapper](https://github.com/DapperLib/Dapper) を使っています。
SQL の実行結果を C# のクラスに詰め替えるだけの小さなライブラリです。

| Classic ASP (ADO) | Razor Pages (Npgsql + Dapper) |
|---|---|
| `Set conn = Server.CreateObject("ADODB.Connection")` : `conn.Open DSN` | `await using var conn = await Db.OpenConnectionAsync();` |
| `Set rs = conn.Execute("SELECT ...")` | `var rows = await conn.QueryAsync<Todo>("SELECT ...");` |
| `rs("title")` | `row.Title` |
| `Do Until rs.EOF … rs.MoveNext : Loop` | `foreach (var row in rows) { … }` |
| `conn.Execute "INSERT ... '" & Replace(s, "'", "''") & "'"` | `await conn.ExecuteAsync("INSERT ... VALUES (@Title)", new { Title = s });` |
| `rs(0)` (件数などの 1 つの値) | `await conn.ExecuteScalarAsync<int>("SELECT count(*) ...")` |
| `rs.PageSize` / `rs.AbsolutePage` | SQL の `LIMIT @Limit OFFSET @Offset` |
| `rs.Close : conn.Close : Set conn = Nothing` | 不要 (`await using` が自動で閉じる) |
| `On Error Resume Next` + `If Err.Number <> 0` | `try { … } catch (PostgresException ex) { … }` |

SQL Server の方言で書かれた SQL を PostgreSQL に移すときの主な違い:

| SQL Server | PostgreSQL |
|---|---|
| `IDENTITY(1,1)` | `GENERATED ALWAYS AS IDENTITY` |
| `BIT` (0 / 1) | `boolean` (false / true) |
| `NVARCHAR(50)` | `varchar(50)` (もともと Unicode) |
| `DATETIME` | `date` (日付のみ) / `timestamptz` (日時) |
| `GETDATE()` | `now()` |
| `SELECT TOP 10 ...` | `... LIMIT 10` |
| `LIKE` (照合順序しだいで大文字小文字を無視) | `ILIKE` (大文字小文字を無視) |
| `ISNULL(a, b)` | `COALESCE(a, b)` |
| `a + b` (文字列の連結) | `a \|\| b` |

SQL Server をそのまま使い続けたい場合は、Npgsql の代わりに `Microsoft.Data.SqlClient` を使えば、Dapper の書き方は同じです。

## COM コンポーネント (BASP21 など) の置き換え先

Linux では COM が使えないため、`Server.CreateObject` で呼んでいた部品は .NET のライブラリに置き換えます。
多くは標準ライブラリだけで足ります。

| やっていたこと | 当時の部品 | .NET での置き換え |
|---|---|---|
| 文字コード変換 (Shift_JIS ⇔ UTF-8) | BASP21 `Kconv`、`ADODB.Stream` | `System.Text.Encoding` (`Csv.cs` 参照) |
| ファイルアップロードの受信 | BASP21 `FormSaveAs` | `IFormFile` をハンドラーの引数や `[BindProperty]` で受け取る |
| メール送信 | BASP21 `SendMail`、CDONTS、CDO | [MailKit](https://github.com/jstedfast/MailKit) |
| 正規表現 | VBScript `RegExp`、BASP21 `Match` | `System.Text.RegularExpressions.Regex` |
| MD5 / Base64 | BASP21 `MD5`、`Base64` | `System.Security.Cryptography`、`Convert.ToBase64String` |
| ファイル操作 | `Scripting.FileSystemObject` | `System.IO.File`、`Directory` |
| HTTP 通信 | `MSXML2.ServerXMLHTTP` | `HttpClient` |
| XML の読み書き | `MSXML2.DOMDocument` | `System.Xml.Linq.XDocument` |
| 連想配列 | `Scripting.Dictionary` | `Dictionary<TKey, TValue>` |

※ このリポジトリで実装しているのは 1 行目 (文字コード変換) だけです。それ以外は置き換え先の案内です。

## 入力チェック

| Classic ASP | Razor Pages |
|---|---|
| `If Len(x) = 0 Then err = "必須です"` | `[Required(ErrorMessage = "{0}は必須です。")]` |
| `If Len(x) > 50 Then ...` | `[StringLength(50)]` |
| `If Not IsNumeric(x) Then ...` | プロパティの型を `int` にする (変換できなければ自動でエラー) |
| `If Not IsDate(x) Then ...` | プロパティの型を `DateOnly` にする |
| `re.Pattern = "..." : If Not re.Test(x)` | `[RegularExpression("...")]` |
| エラーがあれば同じ画面を出し直す | `if (!ModelState.IsValid) return Page();` |
| DB を見てのチェック | `ModelState.AddModelError("Input.EmployeeNo", "...")` |
| エラーメッセージの表示 | `<span asp-validation-for="Input.Name">`、`<div asp-validation-summary="ModelOnly">` |
