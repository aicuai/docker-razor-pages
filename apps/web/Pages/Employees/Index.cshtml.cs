using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;

namespace WebApp.Pages.Employees;

/// <summary>
/// 従業員マスタ一覧の「処理」側。Index.cshtml (画面) と 2 つで 1 ページになる。
/// コンストラクターの引数 (EmployeeRepository) は Program.cs で登録したものが自動で渡される。
/// </summary>
public class IndexModel(EmployeeRepository repository) : PageModel
{
    public const int PageSize = 10;

    // ---- URL から受け取る検索条件 ( /Employees?q=山田&dept=3&retired=true&p=2 ) ----
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true, Name = "dept")]
    public int? DepartmentId { get; set; }

    [BindProperty(SupportsGet = true, Name = "retired")]
    public bool IncludeRetired { get; set; }

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNo { get; set; } = 1;

    // ---- 画面に渡すデータ ----
    public IReadOnlyList<Department> Departments { get; private set; } = [];
    public IReadOnlyList<Employee> Employees { get; private set; } = [];
    public int Total { get; private set; }

    public int TotalPages => (Total + PageSize - 1) / PageSize;
    public int RangeFrom => Total == 0 ? 0 : (PageNo - 1) * PageSize + 1;
    public int RangeTo => Math.Min(PageNo * PageSize, Total);

    private EmployeeSearch Search => new(Keyword, DepartmentId, IncludeRetired);

    // ---- GET /Employees ----
    public async Task OnGetAsync()
    {
        // ?p=abc のような不正な値は無視して 1 ページ目として扱う
        if (!ModelState.IsValid || PageNo < 1) PageNo = 1;

        Departments = await repository.GetDepartmentsAsync();
        (Employees, Total) = await repository.SearchAsync(Search, PageNo, PageSize);

        // 削除などで最終ページが無くなった場合は、存在する最後のページを出す
        if (Employees.Count == 0 && Total > 0)
        {
            PageNo = TotalPages;
            (Employees, Total) = await repository.SearchAsync(Search, PageNo, PageSize);
        }
    }

    // ---- GET /Employees?handler=Csv (検索条件に合う全件を Shift_JIS の CSV でダウンロード) ----
    public async Task<FileContentResult> OnGetCsvAsync()
    {
        var employees = await repository.SearchAllAsync(Search);

        string[] header = ["社員番号", "氏名", "フリガナ", "部署コード", "部署名", "メールアドレス", "入社日", "在籍区分", "備考"];
        var rows = employees.Select(e => new[]
        {
            e.EmployeeNo, e.Name, e.NameKana, e.DepartmentCode, e.DepartmentName, e.Email,
            e.HiredOn?.ToString("yyyy/MM/dd"), e.IsRetired ? "退職" : "在籍", e.Note,
        });

        // ASP: Response.ContentType / Response.AddHeader "Content-Disposition" / Response.BinaryWrite
        return File(Csv.ToShiftJisBytes(header, rows), "text/csv; charset=Shift_JIS", $"employees_{DateTime.Now:yyyyMMdd}.csv");
    }

    // ---- POST /Employees?handler=Delete (id はフォームの hidden 項目から入る) ----
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        TempData["Message"] = await repository.DeleteAsync(id)
            ? "削除しました。"
            : "対象の従業員は既に削除されています。";

        return RedirectToPage(RouteValues(PageNo));
    }

    /// <summary>いまの検索条件を引き継いだリンクを作るための値。ページ番号だけ差し替えられる。</summary>
    public Dictionary<string, string> RouteValues(int? pageNo = null)
    {
        var values = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Keyword)) values["q"] = Keyword;
        if (DepartmentId is { } dept) values["dept"] = dept.ToString();
        if (IncludeRetired) values["retired"] = "true";
        if (pageNo is > 1) values["p"] = pageNo.Value.ToString();
        return values;
    }
}
