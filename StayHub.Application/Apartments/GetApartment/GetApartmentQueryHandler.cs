using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartment;

internal sealed class GetApartmentQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetApartmentQuery, ApartmentResponse>
{
    public async Task<Result<ApartmentResponse>> Handle(
        GetApartmentQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               a.id AS Id,
                               a.owner_id AS OwnerId,
                               a.name AS Name,
                               a.description AS Description,
                               a.price_amount AS PriceAmount,
                               a.price_currency AS PriceCurrency,
                               a.cleaning_fee_amount AS CleaningFeeAmount,
                               a.cleaning_fee_currency AS CleaningFeeCurrency,
                               a.is_active AS IsActive,
                               a.amenities AS Amenities,

                               a.address_country AS Country,
                               a.address_state AS State,
                               a.address_zip_code AS ZipCode,
                               a.address_city AS City,
                               a.address_street AS Street,

                               owner.id AS HostId,
                               owner.first_name || ' ' || owner.last_name AS HostFullName,
                               hp.avatar_url AS HostAvatarUrl,

                               rv.avg_rating AS Rating,
                               COALESCE(rv.review_count, 0) AS ReviewCount

                           FROM apartments AS a

                           JOIN users AS owner
                               ON owner.id = a.owner_id

                           LEFT JOIN user_profiles AS hp
                               ON hp.user_id = owner.id

                           LEFT JOIN LATERAL (
                               SELECT
                                   AVG(r.rating)::float AS avg_rating,
                                   COUNT(*)::int AS review_count
                               FROM reviews AS r
                               WHERE r.apartment_id = a.id
                           ) AS rv ON true

                           WHERE a.id = @ApartmentId;

                           SELECT
                               ai.id AS Id,
                               ai.url AS Url,
                               ai.display_order AS DisplayOrder,
                               ai.is_primary AS IsPrimary
                           FROM apartment_images AS ai
                           WHERE ai.apartment_id = @ApartmentId
                           ORDER BY ai.display_order ASC;

                           SELECT
                               r.id AS Id,
                               u.first_name || ' ' || u.last_name AS ReviewerName,
                               up.avatar_url AS ReviewerAvatarUrl,
                               r.rating AS Rating,
                               r.comment AS Comment,
                               r.created_on_utc AS CreatedOnUtc
                           FROM reviews AS r

                           JOIN users AS u
                               ON u.id = r.user_id

                           LEFT JOIN user_profiles AS up
                               ON up.user_id = u.id

                           WHERE r.apartment_id = @ApartmentId
                           ORDER BY r.created_on_utc DESC
                           LIMIT 3;
                           """;

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new { request.ApartmentId });

        var row = multi
            .Read<ApartmentRow>()
            .SingleOrDefault();

        if (row is null)
        {
            return Result.Failure<ApartmentResponse>(ApartmentErrors.NotFound);
        }

        var apartment = new ApartmentResponse
        {
            Id = row.Id,
            OwnerId = row.OwnerId,
            Name = row.Name,
            Description = row.Description,
            PriceAmount = row.PriceAmount,
            PriceCurrency = row.PriceCurrency,
            CleaningFeeAmount = row.CleaningFeeAmount,
            CleaningFeeCurrency = row.CleaningFeeCurrency,
            IsActive = row.IsActive,
            Amenities = row.Amenities,

            Address = new AddressResponse
            {
                Country = row.Country,
                State = row.State,
                ZipCode = row.ZipCode,
                City = row.City,
                Street = row.Street
            },

            Host = new ApartmentHostResponse
            {
                Id = row.HostId,
                FullName = row.HostFullName,
                AvatarUrl = row.HostAvatarUrl
            },

            Rating = row.Rating,
            ReviewCount = row.ReviewCount
        };

        // A deactivated apartment's public details page must not be reachable by
        // guessing/reusing its id — only the owner or an admin may still view it.
        if (!apartment.IsActive &&
            !userContext.IsAdmin &&
            !userContext.IsOwner(apartment.OwnerId))
        {
            return Result.Failure<ApartmentResponse>(ApartmentErrors.NotFound);
        }

        apartment.Images = multi
            .Read<ApartmentImageResponse>()
            .ToList();

        apartment.RecentReviews = multi
            .Read<ApartmentReviewPreviewResponse>()
            .ToList();

        if (!userContext.IsAuthenticated) return apartment;

        var isFavorited = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM favorite_apartments
                WHERE user_id = @UserId
                  AND apartment_id = @ApartmentId
            )
            """,
            new
            {
                UserId = userContext.UserId,
                request.ApartmentId
            });

        apartment = apartment with { IsFavorited = isFavorited };

        return apartment;
    }

    private sealed class ApartmentRow
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }

        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }

        public decimal PriceAmount { get; init; }
        public string PriceCurrency { get; init; } = string.Empty;

        public decimal CleaningFeeAmount { get; init; }
        public string CleaningFeeCurrency { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public IReadOnlyList<string>? Amenities { get; init; }

        public string Country { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string ZipCode { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Street { get; init; } = string.Empty;

        public Guid HostId { get; init; }
        public string HostFullName { get; init; } = string.Empty;
        public string? HostAvatarUrl { get; init; }

        public double? Rating { get; init; }
        public int ReviewCount { get; init; }
    }
}