using System.Data;
using Dapper;

namespace StayHub.Infrastructure.Data;

internal sealed class EnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum>
{
    public override void SetValue(IDbDataParameter parameter, TEnum? value)
    {
        parameter.Value = Convert.ToInt32(value);
    }

    public override TEnum Parse(object value)
    {
        // Postgres integer columns come back from Npgsql as int (or short, depending on the
        // column type) - Convert.ToInt32 handles either without needing to know which.
        return (TEnum)Enum.ToObject(typeof(TEnum), Convert.ToInt32(value));
    }
}