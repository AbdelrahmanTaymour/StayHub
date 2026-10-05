using Dapper;
using MediatR;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartment;

internal sealed class GetApartmentQueryHandler(
    ISender sender,
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetApartmentQuery, ApartmentResponse>
{
    public async Task<Result<ApartmentResponse>> Handle(
        GetApartmentQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await sender.Send(new CachedGetApartmentQuery(request.ApartmentId), cancellationToken);

        if (cached.IsFailure)
        {
            return Result.Failure<ApartmentResponse>(cached.Error);
        }

        var apartment = cached.Value;

        // Visibility depends on the caller, so it's checked on every request, never inside the cache.
        if (!apartment.IsActive &&
            !userContext.IsAdmin &&
            !userContext.IsOwner(apartment.OwnerId))
        {
            return Result.Failure<ApartmentResponse>(ApartmentErrors.NotFound);
        }

        if (!userContext.IsAuthenticated)
        {
            return apartment;
        }

        using var connection = sqlConnectionFactory.CreateConnection();

        var isFavorited = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM favorite_apartments
                WHERE user_id = @UserId
                  AND apartment_id = @ApartmentId
            )
            """,
            new { UserId = userContext.UserId, request.ApartmentId });

        return apartment with { IsFavorited = isFavorited };
    }
}