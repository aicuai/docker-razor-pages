using System.Text;

namespace WebApp;

/// <summary>
/// Excel でそのまま開ける Shift_JIS の CSV を作る。
/// Classic ASP 時代は BASP21 や ADODB.Stream に頼っていた処理で、
/// .NET では標準ライブラリだけで書ける。
/// </summary>
public static class Csv
{
    /// <summary>Windows の Shift_JIS (コードページ 932)。Program.cs で CodePages を登録済みであること。</summary>
    public static Encoding ShiftJis => Encoding.GetEncoding(932);

    public static byte[] ToShiftJisBytes(IEnumerable<string> header, IEnumerable<IEnumerable<string?>> rows)
        => ShiftJis.GetBytes(ToText(header, rows));

    public static string ToText(IEnumerable<string> header, IEnumerable<IEnumerable<string?>> rows)
    {
        var sb = new StringBuilder();
        AppendLine(sb, header);
        foreach (var row in rows) AppendLine(sb, row);
        return sb.ToString();
    }

    private static void AppendLine(StringBuilder sb, IEnumerable<string?> fields)
    {
        sb.AppendJoin(',', fields.Select(Escape));
        sb.Append("\r\n"); // Excel 向けに改行は CRLF
    }

    /// <summary>1 項目を CSV 用に整形する。</summary>
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        // 「=」などで始まる値を Excel が数式として実行しないよう、先頭に ' を付ける (CSV インジェクション対策)
        if (value[0] is '=' or '+' or '-' or '@') value = "'" + value;

        var needsQuote = value.AsSpan().IndexOfAny(",\"\r\n") >= 0;
        return needsQuote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
