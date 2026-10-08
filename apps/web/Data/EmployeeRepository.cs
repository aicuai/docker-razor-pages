using Dapper;
using Npgsql;

namespace WebApp.Data;

/// <summary>
/// 従業員マスタの SQL をまとめたクラス。
/// Classic ASP でいう「inc/db.asp に関数をまとめて #include する」の置き換え。
/// SQL は手書きのままだが、値は必ず @パラメータ で渡す (文字列連結しない)。
/// </summary>
public sealed class EmployeeRepository(NpgsqlDataSource db)
{
    private const string SelectColumns = """
        SELECT e.id, e.employee_no, e.name, e.name_kana, e.department_id, e.email,
               e.hired_on, e.is_retired, e.note, e.updated_by, e.updated_at,
               d.code AS department_code, d.name AS department_name
        FROM employees e
        JOIN departments d ON d.id = e.department_id
        """;

    private const string WhereClause = """
        WHERE (@Pattern::text IS NULL
               OR e.name ILIKE @Pattern OR e.name_kana ILIKE @Pattern
               OR e.employee_no ILIKE @Pattern OR e.email ILIKE @Pattern)
          AND (@DepartmentId::int IS NULL OR e.department_id = @DepartmentId)
          AND (@IncludeRetired OR NOT e.is_retired)
        """;

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync()
    {
        await using var conn = await db.OpenConnectionAsync();
        var rows = await conn.QueryAsync<Department>("SELECT id, code, name FROM departments ORDER BY code");
        return rows.AsList();
    }

    /// <summary>条件に合う従業員を 1 ページ分だけ取得する。</summary>
    public async Task<(IReadOnlyList<Employee> Items, int Total)> SearchAsync(EmployeeSearch search, int pageNo, int pageSize)
    {
        var args = new
        {
            Pattern = ToLikePattern(search.Keyword),
            search.DepartmentId,
            search.IncludeRetired,
            Limit = pageSize,
            Offset = (Math.Max(pageNo, 1) - 1) * pageSize,
        };

        await using var conn = await db.OpenConnectionAsync();
        var total = await conn.ExecuteScalarAsync<int>($"SELECT count(*) FROM employees e {WhereClause}", args);
        var items = await conn.QueryAsync<Employee>(
            $"{SelectColumns} {WhereClause} ORDER BY e.employee_no LIMIT @Limit OFFSET @Offset", args);
        return (items.AsList(), total);
    }

    /// <summary>条件に合う従業員を全件取得する (CSV 出力用)。</summary>
    public async Task<IReadOnlyList<Employee>> SearchAllAsync(EmployeeSearch search)
    {
        var args = new { Pattern = ToLikePattern(search.Keyword), search.DepartmentId, search.IncludeRetired };
        await using var conn = await db.OpenConnectionAsync();
        var items = await conn.QueryAsync<Employee>($"{SelectColumns} {WhereClause} ORDER BY e.employee_no", args);
        return items.AsList();
    }

    public async Task<Employee?> FindAsync(int id)
    {
        await using var conn = await db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Employee>($"{SelectColumns} WHERE e.id = @id", new { id });
    }

    /// <summary>社員番号が他の行で使われているか。</summary>
    public async Task<bool> EmployeeNoExistsAsync(string employeeNo, int? exceptId)
    {
        await using var conn = await db.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM employees WHERE employee_no = @employeeNo AND (@exceptId::int IS NULL OR id <> @exceptId))",
            new { employeeNo, exceptId });
    }

    public async Task<int> InsertAsync(Employee e)
    {
        await using var conn = await db.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO employees (employee_no, name, name_kana, department_id, email, hired_on, is_retired, note, updated_by)
            VALUES (@EmployeeNo, @Name, @NameKana, @DepartmentId, @Email, @HiredOn, @IsRetired, @Note, @UpdatedBy)
            RETURNING id
            """, e);
    }

    public async Task<bool> UpdateAsync(Employee e)
    {
        await using var conn = await db.OpenConnectionAsync();
        var affected = await conn.ExecuteAsync("""
            UPDATE employees
               SET employee_no = @EmployeeNo, name = @Name, name_kana = @NameKana,
                   department_id = @DepartmentId, email = @Email, hired_on = @HiredOn,
                   is_retired = @IsRetired, note = @Note, updated_by = @UpdatedBy, updated_at = now()
             WHERE id = @Id
            """, e);
        return affected == 1;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = await db.OpenConnectionAsync();
        return await conn.ExecuteAsync("DELETE FROM employees WHERE id = @id", new { id }) == 1;
    }

    /// <summary>
    /// 入力されたキーワードを LIKE 用の「%キーワード%」に変換する。
    /// % や _ をそのまま探せるようにエスケープしておく。
    /// </summary>
    internal static string? ToLikePattern(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return null;
        var escaped = keyword.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        return $"%{escaped}%";
    }
}
