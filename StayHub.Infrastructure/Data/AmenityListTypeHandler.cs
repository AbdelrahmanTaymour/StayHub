using System.Data;
using Dapper;
using StayHub.Domain.Apartments;

namespace StayHub.Infrastructure.Data;

public sealed class AmenityListTypeHandler : SqlMapper.TypeHandler<IReadOnlyList<string>>
{
    public override void SetValue(IDbDataParameter parameter, IReadOnlyList<string>? value)
    {
        parameter.Value = value?.Select(a => a).ToArray();
    }

    public override IReadOnlyList<string> Parse(object value)
    {
        if (value is int[] intValues)
            return intValues
                .Where(v => Enum.IsDefined(typeof(Amenity), v))
                .Select(v => ((Amenity)v).ToString())
                .ToList();

        return [];
    }
}