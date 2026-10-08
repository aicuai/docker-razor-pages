<%@ Language=VBScript CodePage=65001 %>
<!--#include file="inc/conn.asp"-->
<%
' メニュー画面。 Razor 版: Pages/Index.cshtml
Dim pageTitle, errMsg, operatorName, hits
pageTitle = "メニュー"
errMsg = ""

' ---- POST (「設定」ボタン) ----
If Request.ServerVariables("REQUEST_METHOD") = "POST" Then
    operatorName = Trim(Request.Form("OperatorName"))
    If operatorName = "" Then
        errMsg = "担当者名を入力してください。"
    Else
        Session("OperatorName") = operatorName
        Session("Message") = "担当者を「" & operatorName & "」に設定しました。"
        Response.Redirect "default.asp"
    End If
Else
    operatorName = Session("OperatorName")
End If

' ---- アクセスカウンター (全ユーザー共有) ----
Application.Lock
Application("Counter") = Application("Counter") + 1
hits = Application("Counter")
Application.Unlock
%>
<!--#include file="inc/header.asp"-->

<h1>メニュー</h1>

<ul class="menu">
    <li><a href="todo.asp">ToDo</a></li>
    <li><a href="employees.asp">従業員マスタ</a></li>
</ul>

<h2>担当者名</h2>
<form method="post" action="default.asp">
    担当者名
    <input type="text" name="OperatorName" value="<%= H(operatorName) %>" maxlength="50">
    <input type="submit" value="設定">
    <span class="field-error"><%= H(errMsg) %></span>
</form>

<p class="counter">あなたは <b><%= FormatNumber(hits, 0) %></b> 人目の訪問者です。</p>

<!--#include file="inc/footer.asp"-->
