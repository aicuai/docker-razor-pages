<%@ Language=VBScript CodePage=65001 %>
<!--#include file="inc/conn.asp"-->
<%
' ============================================================
'  ToDo 画面。 Razor 版: Pages/Todo.cshtml
'  1 つの .asp が「表示」「追加」「完了切替」「削除」を全部受け持つ、当時の定番構成。
' ============================================================
Dim pageTitle, errMsg, action, show, newTitle, newDueDate, sql, rs, where, overdue
pageTitle = "ToDo"
errMsg = ""

show = Request.QueryString("show")
If show <> "open" And show <> "done" Then show = "all"

OpenDb

' ---- POST の処理。hidden の action で何をするか振り分ける ----
' Razor 版: OnPostAddAsync / OnPostToggleAsync / OnPostDeleteAsync に分かれている
If Request.ServerVariables("REQUEST_METHOD") = "POST" Then
    action = Request.Form("action")

    Select Case action
    Case "add"
        newTitle = Trim(Request.Form("NewTitle"))
        newDueDate = Trim(Request.Form("NewDueDate"))

        If newTitle = "" Then
            errMsg = "件名を入力してください。"
        ElseIf Len(newTitle) > 200 Then
            errMsg = "件名は 200 文字以内で入力してください。"
        ElseIf newDueDate <> "" And Not IsDate(newDueDate) Then
            errMsg = "期限の日付が正しくありません。"
        End If

        If errMsg = "" Then
            ' SQL を文字列連結で組み立てる (SqlStr を通し忘れると SQL インジェクション)
            sql = "INSERT INTO todos (title, due_date) VALUES (" & SqlStr(newTitle) & ", " & SqlStr(newDueDate) & ")"
            conn.Execute sql
            Session("Message") = "「" & newTitle & "」を追加しました。"
            CloseDb
            Response.Redirect "todo.asp?show=" & show
        End If

    Case "toggle"
        conn.Execute "UPDATE todos SET is_done = 1 - is_done WHERE id = " & SqlNum(Request.Form("id"))
        CloseDb
        Response.Redirect "todo.asp?show=" & show

    Case "delete"
        conn.Execute "DELETE FROM todos WHERE id = " & SqlNum(Request.Form("id"))
        Session("Message") = "削除しました。"
        CloseDb
        Response.Redirect "todo.asp?show=" & show
    End Select
End If

' ---- 一覧の取得 ----
where = ""
If show = "open" Then where = " WHERE is_done = 0"
If show = "done" Then where = " WHERE is_done = 1"

Set rs = conn.Execute("SELECT id, title, due_date, is_done, created_at FROM todos" & where & _
                      " ORDER BY is_done, CASE WHEN due_date IS NULL THEN 1 ELSE 0 END, due_date, id")
%>
<!--#include file="inc/header.asp"-->

<h1>ToDo</h1>

<form method="post" action="todo.asp?show=<%= show %>">
    <input type="hidden" name="action" value="add">
    件名 <input type="text" name="NewTitle" value="<%= H(newTitle) %>" maxlength="200" size="40">
    期限 <input type="text" name="NewDueDate" value="<%= H(newDueDate) %>" size="12">
    <input type="submit" value="追加">
    <span class="field-error"><%= H(errMsg) %></span>
</form>

<p class="tabs">
    表示:
    <a href="todo.asp?show=all">すべて</a>
    <a href="todo.asp?show=open">未完了</a>
    <a href="todo.asp?show=done">完了</a>
</p>

<% If rs.EOF Then %>
    <p class="empty">表示する ToDo はありません。</p>
<% Else %>
    <table class="grid">
        <tr><th>状態</th><th>件名</th><th>期限</th><th>登録日時</th><th>操作</th></tr>
<%
    Do Until rs.EOF
        overdue = False
        If Not rs("is_done") And Not IsNull(rs("due_date")) Then
            If rs("due_date") < Date Then overdue = True
        End If
%>
        <tr<% If rs("is_done") Then Response.Write " class=""done""" %>>
            <td><% If rs("is_done") Then %>完了<% Else %>未完了<% End If %></td>
            <td><%= H(rs("title")) %></td>
            <td<% If overdue Then Response.Write " class=""overdue""" %>><%= FmtDate(rs("due_date")) %></td>
            <td><%= FmtDate(rs("created_at")) %> <%= FormatDateTime(rs("created_at"), vbShortTime) %></td>
            <td>
                <form method="post" action="todo.asp?show=<%= show %>" style="display:inline">
                    <input type="hidden" name="action" value="toggle">
                    <input type="hidden" name="id" value="<%= rs("id") %>">
                    <input type="submit" value="<% If rs("is_done") Then %>未完了に戻す<% Else %>完了にする<% End If %>">
                </form>
                <form method="post" action="todo.asp?show=<%= show %>" style="display:inline"
                      onsubmit="return confirm('この ToDo を削除します。よろしいですか?');">
                    <input type="hidden" name="action" value="delete">
                    <input type="hidden" name="id" value="<%= rs("id") %>">
                    <input type="submit" value="削除">
                </form>
            </td>
        </tr>
<%
        rs.MoveNext
    Loop
%>
    </table>
<% End If %>

<%
rs.Close
Set rs = Nothing
CloseDb
%>
<!--#include file="inc/footer.asp"-->
