using Dapper;
using MediatR;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.SearchApartments;

internal sealed class SearchApartmentsQueryHandler(
    ISender sender,
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<SearchApartmentsQuery, PagedResponse<SearchApartmentsResponse>>
{
    public async Task<Result<PagedResponse<SearchApartmentsResponse>>> Handle(
        SearchApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var cachedResult = await sender.Send(
            new CachedSearchApartmentsQuery(
                request.City,
                request.MinPrice,
                request.MaxPrice,
                request.Start,
                request.End,
                request.Page,
                request.PageSize),
            cancellationToken);

        if (cachedResult.IsFailure)
        {
            return Result.Failure<PagedResponse<SearchApartmentsResponse>>(cachedResult.Error);
        }

        var shared = cachedResult.Value;

        var apartmentIds = shared.Items.Select(item => item.Id).ToArray();
        var favoritedIds = await GetFavoritedApartmentIdsAsync(apartmentIds);

        var items = shared.Items
            .Select(item => ToResponse(item, favoritedIds.Contains(item.Id)))
            .ToList();

        return new PagedResponse<SearchApartmentsResponse>
        {
            Items = items,
            Page = shared.Page,
            PageSize = shared.PageSize,
            TotalCount = shared.TotalCount,
            TotalPages = shared.TotalPages
        };
    }

    private async Task<HashSet<Guid>> GetFavoritedApartmentIdsAsync(Guid[] apartmentIds)
    {
        if (!userContext.IsAuthenticated || apartmentIds.Length == 0)
        {
            return [];
        }

        using var connection = sqlConnectionFactory.CreateConnection();

        var favoriteIds = await connection.QueryAsync<Guid>(
            """
            SELECT apartment_id
            FROM favorite_apartments
            WHERE user_id = @UserId
              AND apartment_id = ANY(@ApartmentIds)
            """,
            new { userContext.UserId, ApartmentIds = apartmentIds });

        return favoriteIds.ToHashSet();
    }

    private static SearchApartmentsResponse ToResponse(ApartmentSearchResult item, bool isFavorited) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            City = item.City,
            Country = item.Country,
            PricePerNight = item.PricePerNight,
            TotalPrice = item.TotalPrice,
            Currency = item.Currency,
            PrimaryImageUrl = item.PrimaryImageUrl,
            Rating = item.Rating,
            ReviewCount = item.ReviewCount,
            IsFavorited = isFavorited
        };
}