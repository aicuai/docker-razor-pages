<%@ Language=VBScript CodePage=65001 %>
<!--#include file="inc/conn.asp"-->
<%
' ============================================================
'  従業員の新規登録 / 修正。 Razor 版: Pages/Employees/Edit.cshtml + Edit.cshtml.cs
' ============================================================
Dim pageTitle, id, isNew, errors, rs, rsDept, sql, re
Dim employeeNo, empName, nameKana, departmentId, email, hiredOn, isRetired, note

id = SqlNum(Request.QueryString("id"))
isNew = (id = 0)
If isNew Then pageTitle = "従業員の新規登録" Else pageTitle = "従業員の修正"
errors = ""

OpenDb

If Request.ServerVariables("REQUEST_METHOD") = "POST" Then
    ' ---- 入力値の取得 ----
    employeeNo = Trim(Request.Form("EmployeeNo"))
    empName = Trim(Request.Form("Name"))
    nameKana = Trim(Request.Form("NameKana"))
    departmentId = Request.Form("DepartmentId")
    email = Trim(Request.Form("Email"))
    hiredOn = Trim(Request.Form("HiredOn"))
    isRetired = (Request.Form("IsRetired") = "1")
    note = Trim(Request.Form("Note"))

    ' ---- 入力チェック。1 項目ずつ If を並べる ----
    ' Razor 版は EmployeeInput クラスに [Required] などの属性で宣言する
    Set re = New RegExp

    re.Pattern = "^[A-Z0-9]{4,10}$"
    If employeeNo = "" Then
        errors = errors & "<li>社員番号は必須です。</li>"
    ElseIf Not re.Test(employeeNo) Then
        errors = errors & "<li>社員番号は半角の英大文字・数字 4〜10 桁で入力してください。</li>"
    End If

    If empName = "" Then
        errors = errors & "<li>氏名は必須です。</li>"
    ElseIf Len(empName) > 50 Then
        errors = errors & "<li>氏名は50文字以内で入力してください。</li>"
    End If

    re.Pattern = "^[ァ-ヶー 　]+$"
    If nameKana = "" Then
        errors = errors & "<li>フリガナは必須です。</li>"
    ElseIf Not re.Test(nameKana) Then
        errors = errors & "<li>フリガナは全角カタカナで入力してください。</li>"
    End If

    If Not IsNumeric(departmentId) Then errors = errors & "<li>部署を選択してください。</li>"

    re.Pattern = "^[^@\s]+@[^@\s]+$"
    If email <> "" And Not re.Test(email) Then errors = errors & "<li>メールアドレスの形式が正しくありません。</li>"

    If hiredOn <> "" And Not IsDate(hiredOn) Then errors = errors & "<li>入社日が正しくありません。</li>"
    If Len(note) > 1000 Then errors = errors & "<li>備考は1000文字以内で入力してください。</li>"

    ' ---- 社員番号の重複チェック ----
    If errors = "" Then
        Set rs = conn.Execute("SELECT COUNT(*) FROM employees WHERE employee_no = " & SqlStr(employeeNo) & " AND id <> " & id)
        If rs(0) > 0 Then errors = errors & "<li>この社員番号は既に使われています。</li>"
        rs.Close
    End If

    ' ---- 登録 / 更新 ----
    If errors = "" Then
        If isNew Then
            sql = "INSERT INTO employees (employee_no, name, name_kana, department_id, email, hired_on, is_retired, note, updated_by) VALUES (" & _
                  SqlStr(employeeNo) & ", " & SqlStr(empName) & ", " & SqlStr(nameKana) & ", " & CLng(departmentId) & ", " & _
                  SqlStr(email) & ", " & SqlStr(hiredOn) & ", " & Abs(CInt(isRetired)) & ", " & SqlStr(note) & ", " & _
                  SqlStr(Session("OperatorName")) & ")"
        Else
            sql = "UPDATE employees SET employee_no = " & SqlStr(employeeNo) & ", name = " & SqlStr(empName) & _
                  ", name_kana = " & SqlStr(nameKana) & ", department_id = " & CLng(departmentId) & _
                  ", email = " & SqlStr(email) & ", hired_on = " & SqlStr(hiredOn) & _
                  ", is_retired = " & Abs(CInt(isRetired)) & ", note = " & SqlStr(note) & _
                  ", updated_by = " & SqlStr(Session("OperatorName")) & ", updated_at = GETDATE() WHERE id = " & id
        End If
        conn.Execute sql

        Session("Message") = employeeNo & " " & empName & " を保存しました。"
        CloseDb
        Response.Redirect "employees.asp"
    End If

ElseIf Not isNew Then
    ' ---- 修正画面の初期表示 ----
    Set rs = conn.Execute("SELECT * FROM employees WHERE id = " & id)
    If rs.EOF Then
        CloseDb
        Response.Status = "404 Not Found"
        Response.End
    End If
    employeeNo = rs("employee_no")
    empName = rs("name")
    nameKana = rs("name_kana")
    departmentId = CStr(rs("department_id"))
    email = rs("email")
    hiredOn = FmtDate(rs("hired_on"))
    isRetired = rs("is_retired")
    note = rs("note")
    rs.Close
End If

Set rsDept = conn.Execute("SELECT id, code, name FROM departments ORDER BY code")
%>
<!--#include file="inc/header.asp"-->

<h1><%= H(pageTitle) %></h1>

<% If errors <> "" Then %>
    <ul class="error-summary"><%= errors %></ul>
<% End If %>

<form method="post" action="employee_edit.asp<% If Not isNew Then Response.Write "?id=" & id %>">
    <table>
        <tr><th>社員番号</th><td><input type="text" name="EmployeeNo" value="<%= H(employeeNo) %>" size="12"></td></tr>
        <tr><th>氏名</th><td><input type="text" name="Name" value="<%= H(empName) %>" size="30"></td></tr>
        <tr><th>フリガナ</th><td><input type="text" name="NameKana" value="<%= H(nameKana) %>" size="30"></td></tr>
        <tr><th>部署</th><td>
            <select name="DepartmentId">
                <option value="">(選択してください)</option>
<% Do Until rsDept.EOF %>
                <option value="<%= rsDept("id") %>"<% If CStr(rsDept("id")) = departmentId Then Response.Write " selected" %>><%= H(rsDept("code")) %> <%= H(rsDept("name")) %></option>
<%
    rsDept.MoveNext
Loop
rsDept.Close
%>
            </select>
        </td></tr>
        <tr><th>メールアドレス</th><td><input type="text" name="Email" value="<%= H(email) %>" size="40"></td></tr>
        <tr><th>入社日</th><td><input type="text" name="HiredOn" value="<%= H(hiredOn) %>" size="12"> (例: 2005/04/01)</td></tr>
        <tr><th>退職済み</th><td><input type="checkbox" name="IsRetired" value="1"<% If isRetired Then Response.Write " checked" %>></td></tr>
        <tr><th>備考</th><td><textarea name="Note" rows="3" cols="50"><%= H(note) %></textarea></td></tr>
    </table>
    <input type="submit" value="<% If isNew Then %>登録<% Else %>更新<% End If %>">
    <a href="employees.asp">一覧に戻る</a>
</form>

<% CloseDb %>
<!--#include file="inc/footer.asp"-->
