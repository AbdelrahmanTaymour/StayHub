using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartmentImages;

internal sealed class GetApartmentPhotosQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetApartmentImagesQuery, ApartmentImagesResponse>
{
    public async Task<Result<ApartmentImagesResponse>> Handle(
        GetApartmentImagesQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT a.id AS Id, a.owner_id AS OwnerId
                           FROM apartments AS a
                           WHERE a.id = @ApartmentId
                             AND (a.owner_id = @UserId OR @IsAdmin);

                           SELECT
                               ai.id AS Id,
                               ai.url AS Url,
                               ai.display_order AS DisplayOrder,
                               ai.is_primary AS IsPrimary
                           FROM apartment_images AS ai
                           WHERE ai.apartment_id = @ApartmentId
                           ORDER BY ai.display_order ASC;
                           """;

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new
            {
                request.ApartmentId,
                userContext.UserId,
                userContext.IsAdmin
            });

        var apartment = (await multi.ReadAsync<ApartmentOwnershipRow>())
            .SingleOrDefault();

        if (apartment is null)
        {
            return Result.Failure<ApartmentImagesResponse>(ApartmentErrors.NotFound);
        }

        if (!userContext.IsOwner(apartment.OwnerId) && !userContext.IsAdmin)
        {
            return Result.Failure<ApartmentImagesResponse>(ApartmentErrors.NotAuthorized);
        }

        var photos = (await multi.ReadAsync<ApartmentImageResponse>()).ToList();

        return new ApartmentImagesResponse
        {
            Photos = photos
        };
    }

    private sealed class ApartmentOwnershipRow
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }
    }
}