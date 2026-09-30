using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartmentAmenities;

internal sealed class GetApartmentAmenitiesQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetApartmentAmenitiesQuery, ApartmentAmenitiesResponse>
{
    public async Task<Result<ApartmentAmenitiesResponse>> Handle(
        GetApartmentAmenitiesQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               a.id AS Id,
                               a.owner_id AS OwnerId,
                               a.amenities AS Amenities
                           FROM apartments AS a
                           WHERE a.id = @ApartmentId
                             AND (a.owner_id = @UserId OR @IsAdmin);
                           """;

        var row = await connection.QuerySingleOrDefaultAsync<ApartmentAmenitiesRow>(
            sql,
            new
            {
                request.ApartmentId,
                UserId = userContext.UserId,
                userContext.IsAdmin
            });

        if (row is null)
        {
            return Result.Failure<ApartmentAmenitiesResponse>(ApartmentErrors.NotFound);
        }

        if (!userContext.IsOwner(row.OwnerId) && !userContext.IsAdmin)
        {
            return Result.Failure<ApartmentAmenitiesResponse>(ApartmentErrors.NotAuthorized);
        }

        return new ApartmentAmenitiesResponse
        {
            Amenities = row.Amenities ?? []
        };
    }

    private sealed class ApartmentAmenitiesRow
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }
        public IReadOnlyList<string>? Amenities { get; init; }
    }
}