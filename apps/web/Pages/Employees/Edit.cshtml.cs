using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using WebApp.Data;

namespace WebApp.Pages.Employees;

public class EditModel(EmployeeRepository repository) : PageModel
{
    // フォームの入力値がまとめて入る。[BindProperty] が無いプロパティには外から値が入らない
    // (=「更新者」のような、利用者に書き換えさせたくない項目を守れる)。
    [BindProperty]
    public EmployeeInput Input { get; set; } = new();

    // URL の {id} が入る。null なら新規登録。
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    public bool IsNew => Id is null;
    public IEnumerable<SelectListItem> DepartmentOptions { get; private set; } = [];
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    // ---- GET /Employees/Edit または /Employees/Edit/123 ----
    public async Task<IActionResult> OnGetAsync()
    {
        if (Id is { } id)
        {
            var employee = await repository.FindAsync(id);
            if (employee is null) return NotFound();

            Input = EmployeeInput.From(employee);
            UpdatedAt = employee.UpdatedAt;
            UpdatedBy = employee.UpdatedBy;
        }

        await LoadOptionsAsync();
        return Page();
    }

    // ---- POST (「登録」「更新」ボタン) ----
    public async Task<IActionResult> OnPostAsync()
    {
        // EmployeeInput の [Required] などのチェック結果は、この時点で ModelState に入っている。
        // DB を見ないと分からないチェックだけ、ここで足す。
        if (ModelState.IsValid && await repository.EmployeeNoExistsAsync(Input.EmployeeNo, Id))
        {
            ModelState.AddModelError("Input.EmployeeNo", "この社員番号は既に使われています。");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var employee = Input.ToEmployee();
        employee.UpdatedBy = HttpContext.Session.GetString(SessionKeys.OperatorName); // ASP: Session("OperatorName")

        try
        {
            if (Id is { } id)
            {
                employee.Id = id;
                if (!await repository.UpdateAsync(employee)) return NotFound();
                TempData["Message"] = $"{employee.EmployeeNo} {employee.Name} を更新しました。";
            }
            else
            {
                await repository.InsertAsync(employee);
                TempData["Message"] = $"{employee.EmployeeNo} {employee.Name} を登録しました。";
            }
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // 事前チェックの直後に、他の人が同じ社員番号で登録した場合
            ModelState.AddModelError("Input.EmployeeNo", "この社員番号は既に使われています。");
            await LoadOptionsAsync();
            return Page();
        }

        // 登録後は一覧へ移動する (PRG パターン。F5 で二重登録にならない)
        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        var departments = await repository.GetDepartmentsAsync();
        DepartmentOptions = departments.Select(d => new SelectListItem($"{d.Code} {d.Name}", d.Id.ToString()));
    }
}

/// <summary>
/// 登録・修正フォームの入力項目と入力ルール。
/// Classic ASP で If Len(x) = 0 Then ... と 1 項目ずつ書いていたチェックを、属性で宣言する。
/// </summary>
public sealed class EmployeeInput
{
    private const string RequiredMessage = "{0}は必須です。";
    private const string LengthMessage = "{0}は{1}文字以内で入力してください。";

    [Display(Name = "社員番号")]
    [Required(ErrorMessage = RequiredMessage)]
    [RegularExpression("^[A-Z0-9]{4,10}$", ErrorMessage = "{0}は半角の英大文字・数字 4〜10 桁で入力してください。")]
    public string EmployeeNo { get; set; } = "";

    [Display(Name = "氏名")]
    [Required(ErrorMessage = RequiredMessage)]
    [StringLength(50, ErrorMessage = LengthMessage)]
    public string Name { get; set; } = "";

    [Display(Name = "フリガナ")]
    [Required(ErrorMessage = RequiredMessage)]
    [StringLength(50, ErrorMessage = LengthMessage)]
    [RegularExpression("^[ァ-ヶー 　]+$", ErrorMessage = "{0}は全角カタカナで入力してください。")]
    public string NameKana { get; set; } = "";

    [Display(Name = "部署")]
    [Required(ErrorMessage = "{0}を選択してください。")]
    public int? DepartmentId { get; set; }

    [Display(Name = "メールアドレス")]
    [EmailAddress(ErrorMessage = "{0}の形式が正しくありません。")]
    [StringLength(254, ErrorMessage = LengthMessage)]
    public string? Email { get; set; }

    [Display(Name = "入社日")]
    [DataType(DataType.Date)]
    public DateOnly? HiredOn { get; set; }

    [Display(Name = "退職済み")]
    public bool IsRetired { get; set; }

    [Display(Name = "備考")]
    [StringLength(1000, ErrorMessage = LengthMessage)]
    public string? Note { get; set; }

    public static EmployeeInput From(Employee e) => new()
    {
        EmployeeNo = e.EmployeeNo,
        Name = e.Name,
        NameKana = e.NameKana,
        DepartmentId = e.DepartmentId,
        Email = e.Email,
        HiredOn = e.HiredOn,
        IsRetired = e.IsRetired,
        Note = e.Note,
    };

    public Employee ToEmployee() => new()
    {
        EmployeeNo = EmployeeNo.Trim(),
        Name = Name.Trim(),
        NameKana = NameKana.Trim(),
        DepartmentId = DepartmentId!.Value,
        Email = Email?.Trim(),
        HiredOn = HiredOn,
        IsRetired = IsRetired,
        Note = Note?.Trim(),
    };
}
