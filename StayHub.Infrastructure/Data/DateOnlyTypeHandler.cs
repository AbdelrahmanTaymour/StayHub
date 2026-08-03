using System.Data;
using Dapper;

namespace StayHub.Infrastructure.Data;

/// <summary>
///     Custom Dapper type handler for mapping database date values to .NET <see cref="DateOnly" />.
/// </summary>
/// <remarks>
///     <para>
///         <b>Why is pattern matching used in <see cref="Parse" />?</b><br />
///         Different ADO.NET database providers return date columns as different CLR types:
///         <list type="bullet">
///             <item>
///                 <term>
///                     <b>Npgsql (PostgreSQL):</b>
///                 </term>
///                 <description>Modern versions return PostgreSQL <c>date</c> directly as <see cref="DateOnly" />.</description>
///             </item>
///             <item>
///                 <term>
///                     <b>Microsoft.Data.SqlClient (SQL Server):</b>
///                 </term>
///                 <description>
///                     Legacy drivers or standard SQL Server mapping return <c>date</c> as
///                     <see cref="DateTime" />.
///                 </description>
///             </item>
///         </list>
///         Direct casting like <c>(DateTime)value</c> will throw an <see cref="InvalidCastException" /> when using Npgsql.
///         This pattern-matching switch seamlessly handles both provider types.
///     </para>
/// </remarks>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => throw new ArgumentException($"Cannot convert {value?.GetType().Name} to DateOnly.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value;
    }
}