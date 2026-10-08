<%@ Language=VBScript CodePage=65001 %>
<!--#include file="inc/conn.asp"-->
<!--#include file="inc/employee_where.asp"-->
<%
' ============================================================
'  従業員マスタの CSV 出力 (Shift_JIS)。 Razor 版: Index.cshtml.cs の OnGetCsvAsync + Csv.cs
'  Response.CodePage を 932 に切り替えると、Response.Write した文字列が Shift_JIS で出力される。
'  (当時は BASP21 の Kconv や ADODB.Stream で変換することも多かった)
' ============================================================
Dim rs, sql, retired

' CSV の 1 項目を整形する。カンマ・引用符・改行を含むときだけ " で囲む。
Function CsvField(ByVal v)
    If IsNull(v) Then v = ""
    v = CStr(v)
    If InStr(v, ",") > 0 Or InStr(v, """") > 0 Or InStr(v, vbCr) > 0 Or InStr(v, vbLf) > 0 Then
        v = """" & Replace(v, """", """""") & """"
    End If
    CsvField = v
End Function

retired = (Request.QueryString("retired") = "true")

OpenDb
sql = "SELECT e.employee_no, e.name, e.name_kana, d.code, d.name AS department_name, e.email, e.hired_on, e.is_retired, e.note" & _
      " FROM employees e INNER JOIN departments d ON d.id = e.department_id" & _
      EmployeeWhere(Trim(Request.QueryString("q")), Request.QueryString("dept"), retired) & " ORDER BY e.employee_no"
Set rs = conn.Execute(sql)

Response.Buffer = True
Response.CodePage = 932
Response.Charset = "Shift_JIS"
Response.ContentType = "text/csv"
Response.AddHeader "Content-Disposition", "attachment; filename=employees.csv"

Response.Write "社員番号,氏名,フリガナ,部署コード,部署名,メールアドレス,入社日,在籍区分,備考" & vbCrLf

Do Until rs.EOF
    Response.Write CsvField(rs("employee_no")) & "," & CsvField(rs("name")) & "," & CsvField(rs("name_kana")) & "," & _
                   CsvField(rs("code")) & "," & CsvField(rs("department_name")) & "," & CsvField(rs("email")) & "," & _
                   FmtDate(rs("hired_on")) & ","
    If rs("is_retired") Then Response.Write "退職" Else Response.Write "在籍"
    Response.Write "," & CsvField(rs("note")) & vbCrLf
    rs.MoveNext
Loop

rs.Close
CloseDb
Response.End
%>
