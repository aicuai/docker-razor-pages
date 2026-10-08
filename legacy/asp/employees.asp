<%@ Language=VBScript CodePage=65001 %>
<!--#include file="inc/conn.asp"-->
<!--#include file="inc/employee_where.asp"-->
<%
' ============================================================
'  従業員マスタ一覧。 Razor 版: Pages/Employees/Index.cshtml + Index.cshtml.cs
' ============================================================
Const PAGE_SIZE = 10
Const adUseClient = 3
Const adOpenStatic = 3
Const adLockReadOnly = 1

Dim pageTitle, q, dept, retired, pageNo, rs, rsDept, sql, total, totalPages, i, n, query
pageTitle = "従業員マスタ"

q = Trim(Request.QueryString("q"))
dept = Request.QueryString("dept")
retired = (Request.QueryString("retired") = "true")
pageNo = SqlNum(Request.QueryString("p"))
If pageNo < 1 Then pageNo = 1

' 検索条件をリンクに引き継ぐための文字列
query = "q=" & Server.URLEncode(q) & "&dept=" & Server.URLEncode(dept)
If retired Then query = query & "&retired=true"

OpenDb

' ---- 削除。GET のリンクを踏むだけで消える (当時ありがち。今やってはいけない) ----
' Razor 版は POST + 偽造防止トークン (OnPostDeleteAsync)
If Request.QueryString("del") <> "" Then
    conn.Execute "DELETE FROM employees WHERE id = " & SqlNum(Request.QueryString("del"))
    Session("Message") = "削除しました。"
    CloseDb
    Response.Redirect "employees.asp?" & query & "&p=" & pageNo
End If

' ---- 一覧の取得。ADO のページング機能 (PageSize / AbsolutePage) を使う ----
sql = "SELECT e.id, e.employee_no, e.name, e.name_kana, e.email, e.hired_on, e.is_retired, d.name AS department_name" & _
      " FROM employees e INNER JOIN departments d ON d.id = e.department_id" & _
      EmployeeWhere(q, dept, retired) & " ORDER BY e.employee_no"

Set rs = Server.CreateObject("ADODB.Recordset")
rs.CursorLocation = adUseClient
rs.PageSize = PAGE_SIZE
rs.Open sql, conn, adOpenStatic, adLockReadOnly

total = rs.RecordCount
totalPages = rs.PageCount
If pageNo > totalPages And totalPages > 0 Then pageNo = totalPages
If total > 0 Then rs.AbsolutePage = pageNo

Set rsDept = conn.Execute("SELECT id, name FROM departments ORDER BY code")
%>
<!--#include file="inc/header.asp"-->

<h1>従業員マスタ</h1>

<form method="get" action="employees.asp" class="search-form">
    キーワード <input type="text" name="q" value="<%= H(q) %>" size="30">
    部署
    <select name="dept">
        <option value="">(すべて)</option>
<% Do Until rsDept.EOF %>
        <option value="<%= rsDept("id") %>"<% If CStr(rsDept("id")) = dept Then Response.Write " selected" %>><%= H(rsDept("name")) %></option>
<%
    rsDept.MoveNext
Loop
rsDept.Close
%>
    </select>
    <input type="checkbox" name="retired" value="true"<% If retired Then Response.Write " checked" %>> 退職者を含む
    <input type="submit" value="検索">
    <a href="employees.asp">条件クリア</a>
</form>

<p>
    <a href="employee_edit.asp">新規登録</a>
    <a href="employees_csv.asp?<%= query %>">CSV 出力 (Shift_JIS)</a>
    <%= total %> 件
</p>

<% If total = 0 Then %>
    <p class="empty">条件に合う従業員はいません。</p>
<% Else %>
    <table class="grid">
        <tr><th>社員番号</th><th>氏名</th><th>フリガナ</th><th>部署</th><th>メール</th><th>入社日</th><th>在籍</th><th>操作</th></tr>
<%
    n = 0
    Do Until rs.EOF Or n >= PAGE_SIZE
%>
        <tr>
            <td><%= H(rs("employee_no")) %></td>
            <td><%= H(rs("name")) %></td>
            <td><%= H(rs("name_kana")) %></td>
            <td><%= H(rs("department_name")) %></td>
            <td><%= H(rs("email")) %></td>
            <td><%= FmtDate(rs("hired_on")) %></td>
            <td><% If rs("is_retired") Then %>退職<% Else %>在籍<% End If %></td>
            <td>
                <a href="employee_edit.asp?id=<%= rs("id") %>">修正</a>
                <a href="employees.asp?<%= query %>&p=<%= pageNo %>&del=<%= rs("id") %>"
                   onclick="return confirm('削除します。よろしいですか?');">削除</a>
            </td>
        </tr>
<%
        n = n + 1
        rs.MoveNext
    Loop
%>
    </table>

    <p class="pager">
<% For i = 1 To totalPages %>
<%   If i = pageNo Then %>
        <b><%= i %></b>
<%   Else %>
        <a href="employees.asp?<%= query %>&p=<%= i %>"><%= i %></a>
<%   End If %>
<% Next %>
    </p>
<% End If %>

<%
rs.Close
Set rs = Nothing
CloseDb
%>
<!--#include file="inc/footer.asp"-->
