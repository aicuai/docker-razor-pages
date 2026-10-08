<%
' ============================================================
'  DB 接続と共通関数。各ページの先頭で #include して使う。
'  Razor 版: Program.cs (接続の登録) / Data/EmployeeRepository.cs
' ============================================================
Option Explicit

' 接続文字列がソースに直書き (当時ありがち)。Razor 版は appsettings.json / 環境変数。
Const CONN_STR = "Provider=SQLOLEDB;Data Source=(local);Initial Catalog=aspdemo;User ID=app;Password=app;"

Dim conn

Sub OpenDb()
    Set conn = Server.CreateObject("ADODB.Connection")
    conn.Open CONN_STR
End Sub

Sub CloseDb()
    If Not conn Is Nothing Then
        If conn.State = 1 Then conn.Close
        Set conn = Nothing
    End If
End Sub

' 文字列を SQL に埋め込むための関数。' を '' に置き換えて前後を ' で囲む。
' ※ 通し忘れると SQL インジェクションになる。Razor 版は @パラメータ を使うので不要。
Function SqlStr(ByVal s)
    If IsNull(s) Or s = "" Then
        SqlStr = "NULL"
    Else
        SqlStr = "N'" & Replace(s, "'", "''") & "'"
    End If
End Function

' 数値を SQL に埋め込む。数値でなければ 0。
Function SqlNum(ByVal s)
    If IsNumeric(s) Then SqlNum = CLng(s) Else SqlNum = 0
End Function

' HTML エスケープ。画面に出す値は必ずこれを通す (通し忘れると XSS)。
' Razor 版は @値 と書くだけで自動エスケープされるので不要。
Function H(ByVal s)
    If IsNull(s) Then H = "" Else H = Server.HTMLEncode(CStr(s))
End Function

' 日付を yyyy/MM/dd にそろえる
Function FmtDate(ByVal d)
    If IsNull(d) Or d = "" Then
        FmtDate = ""
    Else
        FmtDate = Year(d) & "/" & Right("0" & Month(d), 2) & "/" & Right("0" & Day(d), 2)
    End If
End Function
%>
