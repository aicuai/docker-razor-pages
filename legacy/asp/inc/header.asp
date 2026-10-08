<%
' 全画面共通のヘッダー。呼び出す側で pageTitle を先に代入しておく。
' Razor 版: Pages/Shared/_Layout.cshtml の前半
Response.Charset = "UTF-8"
%>
<!DOCTYPE html>
<html lang="ja">
<head>
<meta charset="utf-8">
<title><%= H(pageTitle) %> | 社内業務システム</title>
<link rel="stylesheet" href="site.css">
</head>
<body>
<div class="site-header">
    <a class="brand" href="default.asp">社内業務システム</a>
    <a href="default.asp">メニュー</a>
    <a href="todo.asp">ToDo</a>
    <a href="employees.asp">従業員マスタ</a>
    <span class="operator">担当:
<% If Session("OperatorName") = "" Then %>
        未設定
<% Else %>
        <%= H(Session("OperatorName")) %>
<% End If %>
    </span>
</div>
<div class="main">
<% If Session("Message") <> "" Then %>
    <p class="flash"><%= H(Session("Message")) %></p>
<%
    Session("Message") = ""   ' 1 回表示したら消す。Razor 版: TempData
End If
%>
