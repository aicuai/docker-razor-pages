using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApp.Tests;

/// <summary>
/// 画面を実際に HTTP で叩く結合テスト。ブラウザーでの操作 (開く → 入力 → 送信) をそのままなぞる。
/// </summary>
[Collection(AppCollection.Name)]
public class PageTests(AppFactory factory)
{
    [Theory]
    [InlineData("/", "メニュー")]
    [InlineData("/Todo", "ToDo")]
    [InlineData("/Employees", "従業員マスタ")]
    [InlineData("/Employees/Edit", "従業員の新規登録")]
    public async Task Pages_Open(string url, string heading)
    {
        var html = await factory.CreateBrowser().GetStringAsync(url);

        Assert.Contains($"<h1>{heading}</h1>", html);
    }

    [Fact]
    public async Task Menu_OperatorName_IsKeptInSession()
    {
        var browser = factory.CreateBrowser();

        var response = await browser.PostFormAsync("/", "/", new() { ["OperatorName"] = "テスト担当" });
        response.AssertRedirect();

        // 別のページに移動しても、ヘッダーに担当者名が出る
        Assert.Contains("担当: テスト担当", await browser.GetStringAsync("/Todo"));
    }

    [Fact]
    public async Task Todo_Add_Toggle_Delete()
    {
        var browser = factory.CreateBrowser();
        var title = $"テスト用 {Guid.NewGuid():N}";

        (await browser.PostFormAsync("/Todo", "/Todo?handler=Add", new() { ["NewTitle"] = title, ["NewDueDate"] = "2030-01-31" }))
            .AssertRedirect();

        var html = await browser.GetStringAsync("/Todo?show=open");
        Assert.Contains(title, html);
        Assert.Contains("2030/01/31", html);

        var id = await FindTodoIdAsync(title);
        (await browser.PostFormAsync("/Todo", $"/Todo?handler=Toggle&id={id}", [])).AssertRedirect();
        Assert.DoesNotContain(title, await browser.GetStringAsync("/Todo?show=open"));
        Assert.Contains(title, await browser.GetStringAsync("/Todo?show=done"));

        (await browser.PostFormAsync("/Todo", $"/Todo?handler=Delete&id={id}", [])).AssertRedirect();
        Assert.DoesNotContain(title, await browser.GetStringAsync("/Todo"));
    }

    [Fact]
    public async Task Todo_EmptyTitle_ShowsError()
    {
        var response = await factory.CreateBrowser()
            .PostFormAsync("/Todo", "/Todo?handler=Add", new() { ["NewTitle"] = "   " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("件名を入力してください。", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Todo_Title_IsHtmlEscaped()
    {
        var browser = factory.CreateBrowser();
        var marker = Guid.NewGuid().ToString("N");

        await browser.PostFormAsync("/Todo", "/Todo?handler=Add", new() { ["NewTitle"] = $"<script>alert('{marker}')</script>" });

        var html = await browser.GetStringAsync("/Todo");
        Assert.DoesNotContain($"<script>alert('{marker}')", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var response = await factory.CreateBrowser()
            .PostAsync("/Todo?handler=Add", new FormUrlEncodedContent(new Dictionary<string, string> { ["NewTitle"] = "CSRF" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Employee_Create_Search_Edit_Delete()
    {
        var browser = factory.CreateBrowser();
        await browser.PostFormAsync("/", "/", new() { ["OperatorName"] = "結合テスト" });

        var no = "T" + Random.Shared.Next(100000, 999999);
        var departmentId = await FirstDepartmentIdAsync();
        var fields = new Dictionary<string, string>
        {
            ["Input.EmployeeNo"] = no,
            ["Input.Name"] = "試験 太郎",
            ["Input.NameKana"] = "シケン タロウ",
            ["Input.DepartmentId"] = departmentId.ToString(),
            ["Input.Email"] = "shiken@example.co.jp",
            ["Input.HiredOn"] = "2024-04-01",
            ["Input.IsRetired"] = "false",
            ["Input.Note"] = "テストで作成",
        };

        // 登録
        (await browser.PostFormAsync("/Employees/Edit", "/Employees/Edit", fields)).AssertRedirect();

        // 検索で見つかる
        var list = await browser.GetStringAsync($"/Employees?q={no}");
        Assert.Contains("<td>試験 太郎</td>", list);
        Assert.Contains("2024/04/01", list);

        // 同じ社員番号では登録できない
        var duplicate = await browser.PostFormAsync("/Employees/Edit", "/Employees/Edit", fields);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Contains("この社員番号は既に使われています。", await duplicate.Content.ReadAsStringAsync());

        // 修正 (退職にする) → 既定の一覧からは消え、「退職者を含む」で出る。更新者も記録される
        var id = await FindEmployeeIdAsync(no);
        fields["Input.IsRetired"] = "true";
        (await browser.PostFormAsync($"/Employees/Edit/{id}", $"/Employees/Edit/{id}", fields)).AssertRedirect();
        Assert.DoesNotContain("<td>試験 太郎</td>", await browser.GetStringAsync($"/Employees?q={no}"));
        Assert.Contains("<td>試験 太郎</td>", await browser.GetStringAsync($"/Employees?q={no}&retired=true"));
        Assert.Contains("(結合テスト)", await browser.GetStringAsync($"/Employees/Edit/{id}"));

        // 削除 (一覧に実際に出力された削除フォームの送信先と hidden 項目をそのまま使う)
        var listHtml = await browser.GetStringAsync($"/Employees?q={no}&retired=true");
        var form = Regex.Match(listHtml, "<form method=\"post\"[^>]*action=\"([^\"]*handler=Delete[^\"]*)\"[^>]*>\\s*<input type=\"hidden\" name=\"id\" value=\"(\\d+)\"");
        Assert.True(form.Success, "削除フォームが見つかりません。");
        Assert.Equal(id.ToString(), form.Groups[2].Value);
        var deleted = await browser.PostAsync(WebUtility.HtmlDecode(form.Groups[1].Value), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = form.Groups[2].Value,
            ["__RequestVerificationToken"] = listHtml.AntiforgeryToken(),
        }));
        deleted.AssertRedirect();
        Assert.Contains("削除しました。", await browser.GetStringAsync(deleted.Headers.Location!.ToString()));
        Assert.DoesNotContain("<td>試験 太郎</td>", await browser.GetStringAsync($"/Employees?q={no}&retired=true"));
    }

    [Fact]
    public async Task Employee_InvalidInput_ShowsJapaneseMessages()
    {
        var response = await factory.CreateBrowser().PostFormAsync("/Employees/Edit", "/Employees/Edit", new()
        {
            ["Input.EmployeeNo"] = "abc",
            ["Input.Name"] = "",
            ["Input.NameKana"] = "やまだ",
            ["Input.DepartmentId"] = "",
            ["Input.Email"] = "not-an-email",
            ["Input.HiredOn"] = "きのう",
        });

        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("社員番号は半角の英大文字・数字 4〜10 桁で入力してください。", html);
        Assert.Contains("氏名は必須です。", html);
        Assert.Contains("フリガナは全角カタカナで入力してください。", html);
        Assert.Contains("部署を選択してください。", html);
        Assert.Contains("メールアドレスの形式が正しくありません。", html);
        Assert.Contains("入社日の値「きのう」は正しくありません。", html);
    }

    [Fact]
    public async Task Employee_List_PagesByTen()
    {
        var browser = factory.CreateBrowser();

        var page1 = await browser.GetStringAsync("/Employees?retired=true");
        Assert.Contains("E0001", page1);
        Assert.DoesNotContain("E0011", page1);

        var page2 = await browser.GetStringAsync("/Employees?retired=true&p=2");
        Assert.Contains("E0011", page2);
        Assert.DoesNotContain("E0001<", page2);
    }

    [Fact]
    public async Task Employee_Csv_IsShiftJis()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var response = await factory.CreateBrowser().GetAsync("/Employees?handler=Csv&q=E0001");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var text = Encoding.GetEncoding(932).GetString(bytes);

        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Shift_JIS", response.Content.Headers.ContentType?.CharSet);
        Assert.EndsWith(".csv", response.Content.Headers.ContentDisposition?.FileName);
        Assert.StartsWith("社員番号,氏名,フリガナ,部署コード,部署名,メールアドレス,入社日,在籍区分,備考\r\n", text);
        Assert.Contains("E0001,山田 太郎,ヤマダ タロウ,100,総務部,yamada@example.co.jp,1998/04/01,在籍,総務部長\r\n", text);
    }

    [Fact]
    public async Task Employee_Search_TreatsWildcardsAsPlainText()
    {
        var html = await factory.CreateBrowser().GetStringAsync("/Employees?q=%25&retired=true");

        Assert.Contains("条件に合う従業員はいません。", html);
    }

    private static async Task<int> FindTodoIdAsync(string title) =>
        await ScalarAsync("SELECT id FROM todos WHERE title = @value", title);

    private static async Task<int> FindEmployeeIdAsync(string employeeNo) =>
        await ScalarAsync("SELECT id FROM employees WHERE employee_no = @value", employeeNo);

    private static async Task<int> FirstDepartmentIdAsync() =>
        await ScalarAsync("SELECT min(id) FROM departments WHERE @value = ''", "");

    private static async Task<int> ScalarAsync(string sql, string value)
    {
        await using var conn = new Npgsql.NpgsqlConnection(AppFactory.ConnectionString);
        return await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn, sql, new { value });
    }
}
