using System.Data;
using Dapper;

namespace WebApp.Data;

/// <summary>
/// PostgreSQL の date 列と C# の DateOnly を Dapper に橋渡しさせる設定。
/// (時刻を持たない「入社日」「期限」を DateTime で持つと 0:00 やタイムゾーンで事故りやすい)
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value) => parameter.Value = value;

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => DateOnly.Parse(value.ToString()!, System.Globalization.CultureInfo.InvariantCulture),
    };
}
