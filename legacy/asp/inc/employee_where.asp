<%
' 従業員検索の WHERE 句を組み立てる。一覧 (employees.asp) と CSV (employees_csv.asp) で共用。
' Razor 版: Data/EmployeeRepository.cs の WhereClause (こちらは固定の SQL + パラメータ)
Function EmployeeWhere(ByVal q, ByVal dept, ByVal retired)
    Dim w, like_
    w = " WHERE 1 = 1"

    If q <> "" Then
        ' LIKE のワイルドカードを無効化してから ' をエスケープ
        like_ = Replace(Replace(Replace(q, "[", "[[]"), "%", "[%]"), "_", "[_]")
        like_ = "N'%" & Replace(like_, "'", "''") & "%'"
        w = w & " AND (e.name LIKE " & like_ & " OR e.name_kana LIKE " & like_ & _
                " OR e.employee_no LIKE " & like_ & " OR e.email LIKE " & like_ & ")"
    End If

    If IsNumeric(dept) Then w = w & " AND e.department_id = " & CLng(dept)
    If Not retired Then w = w & " AND e.is_retired = 0"

    EmployeeWhere = w
End Function
%>
