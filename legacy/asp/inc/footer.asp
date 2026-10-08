<%
' 全画面共通のフッター。 Razor 版: Pages/Shared/_Layout.cshtml の後半
%>
</div>
<div class="site-footer">
    <%= H(Request.ServerVariables("SERVER_SOFTWARE")) %> / <%= ScriptEngine %> <%= ScriptEngineMajorVersion %>.<%= ScriptEngineMinorVersion %>
</div>
</body>
</html>
