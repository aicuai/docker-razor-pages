namespace WebApp.Data;

// テーブル 1 行分を表すクラス。
// Classic ASP では rs("title") のように列名を文字列で指定していたが、
// ここではプロパティになるので、打ち間違いはコンパイル時に見つかる。

public sealed class Todo
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class Employee
{
    public int Id { get; set; }
    public string EmployeeNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameKana { get; set; } = "";
    public int DepartmentId { get; set; }
    public string? Email { get; set; }
    public DateOnly? HiredOn { get; set; }
    public bool IsRetired { get; set; }
    public string? Note { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }

    // departments を JOIN して取ってくる表示用の列
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
}

/// <summary>従業員一覧の検索条件。</summary>
public sealed record EmployeeSearch(string? Keyword, int? DepartmentId, bool IncludeRetired);
