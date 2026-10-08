using System.Text;

namespace WebApp.Tests;

public class CsvTests
{
    static CsvTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("山田 太郎", "山田 太郎")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("彼は\"OK\"と言った", "\"彼は\"\"OK\"\"と言った\"")]
    [InlineData("1行目\r\n2行目", "\"1行目\r\n2行目\"")]
    public void Escape_QuotesOnlyWhenNeeded(string? input, string expected)
    {
        Assert.Equal(expected, Csv.Escape(input));
    }

    [Theory]
    [InlineData("=SUM(A1:A9)", "'=SUM(A1:A9)")]
    [InlineData("+81-3-0000-0000", "'+81-3-0000-0000")]
    [InlineData("-1", "'-1")]
    [InlineData("@cmd", "'@cmd")]
    public void Escape_NeutralizesFormulaLikeValues(string input, string expected)
    {
        Assert.Equal(expected, Csv.Escape(input));
    }

    [Fact]
    public void ToText_UsesCrLfAndCommas()
    {
        var text = Csv.ToText(["社員番号", "氏名"], [["E0001", "山田 太郎"], ["E0002", null]]);

        Assert.Equal("社員番号,氏名\r\nE0001,山田 太郎\r\nE0002,\r\n", text);
    }

    [Fact]
    public void ToShiftJisBytes_IsReadableAsCp932_AndNotUtf8()
    {
        var bytes = Csv.ToShiftJisBytes(["氏名"], [["髙橋 ﾀﾛｳ"]]);

        // 「氏」は Shift_JIS で 0x8E 0x81
        Assert.Equal([0x8E, 0x81], bytes[..2]);
        Assert.Equal("氏名\r\n髙橋 ﾀﾛｳ\r\n", Encoding.GetEncoding(932).GetString(bytes));
    }
}
