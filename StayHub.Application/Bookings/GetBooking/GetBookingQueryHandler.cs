using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Application.Apartments.GetApartment;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Bookings.GetBooking;

internal sealed class GetBookingQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService) : IQueryHandler<GetBookingQuery, BookingResponse>
{
    public async Task<Result<BookingResponse>> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        // Enforce guest, owner, or admin access in the query itself.
        const string sql = """
                           SELECT
                               b.id AS Id,
                               b.user_id AS GuestId,
                               b.status AS Status,
                               b.created_on_utc AS CreatedOnUtc,
                               GREATEST(
                                   b.created_on_utc,
                                   b.confirmed_on_utc,
                                   b.rejected_on_utc,
                                   b.completed_on_utc,
                                   b.cancelled_on_utc) AS UpdatedOnUtc,

                               a.id AS ApartmentId,
                               a.name AS ApartmentName,
                               img.key AS ApartmentImageKey,
                               a.address_country AS Country,
                               a.address_state AS State,
                               a.address_zip_code AS ZipCode,
                               a.address_city AS City,
                               a.address_street AS Street,

                               b.duration_start AS DurationStart,
                               b.duration_end AS DurationEnd,

                               b.total_price_currency AS Currency,
                               b.price_for_period_amount AS PriceForPeriodAmount,
                               b.cleaning_fee_amount AS CleaningFeeAmount,
                               b.amenities_up_charge_amount AS AmenitiesUpChargeAmount,
                               b.total_price_amount AS TotalPriceAmount,
                               
                               p.status AS PaymentStatus,
                               
                               EXISTS (
                                   SELECT 1
                                   FROM reviews r
                                   WHERE r.booking_id = b.id
                               ) AS HasReview,

                               owner.id AS HostId,
                               owner.first_name || ' ' || owner.last_name AS HostFullName,
                               owner_profile.avatar_key AS HostAvatarKey,

                               c.id AS ConversationId

                           FROM bookings b

                           JOIN apartments a
                               ON a.id = b.apartment_id

                           JOIN users owner
                               ON owner.id = a.owner_id

                           LEFT JOIN user_profiles owner_profile
                               ON owner_profile.user_id = owner.id

                           LEFT JOIN apartment_images img
                               ON img.apartment_id = a.id
                               AND img.is_primary = true

                           LEFT JOIN conversations c
                               ON c.apartment_id = b.apartment_id
                               AND c.guest_id = b.user_id
                               AND c.owner_id = a.owner_id

                           LEFT JOIN LATERAL (
                               SELECT p.status
                               FROM payments p
                               WHERE p.booking_id = b.id
                               ORDER BY p.created_on_utc DESC, p.id DESC
                               LIMIT 1
                           ) p ON TRUE

                           WHERE b.id = @BookingId
                             AND (b.user_id = @UserId OR a.owner_id = @UserId OR @IsAdmin = TRUE)
                           """;

        var row = await connection.QueryFirstOrDefaultAsync<BookingRow>(
            sql,
            new
            {
                request.BookingId,
                userContext.UserId,
                userContext.IsAdmin
            });

        if (row is null)
        {
            return Result.Failure<BookingResponse>(BookingErrors.NotFound);
        }

        return await ToBookingResponseAsync(row, cancellationToken);
    }

    private async Task<BookingResponse> ToBookingResponseAsync(
        BookingRow row,
        CancellationToken cancellationToken)
    {
        var apartmentImageUrlTask = string.IsNullOrWhiteSpace(row.ApartmentImageKey)
            ? Task.FromResult<string?>(null)!
            : fileStorageService.GeneratePresignedUrlAsync(row.ApartmentImageKey, cancellationToken)!;

        var hostAvatarUrlTask = string.IsNullOrWhiteSpace(row.HostAvatarKey)
            ? Task.FromResult<string?>(null)!
            : fileStorageService.GeneratePresignedUrlAsync(row.HostAvatarKey, cancellationToken)!;

        await Task.WhenAll(apartmentImageUrlTask, hostAvatarUrlTask);

        var nights = row.DurationEnd.DayNumber - row.DurationStart.DayNumber;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new BookingResponse
        {
            Id = row.Id,
            Status = row.Status,
            PaymentStatus = row.PaymentStatus,

            CanCancel = row.GuestId == userContext.UserId
                        && row.Status is BookingStatus.Reserved or BookingStatus.Confirmed
                        && row.DurationStart > today,

            CreatedOnUtc = row.CreatedOnUtc,
            UpdatedOnUtc = row.UpdatedOnUtc,

            ApartmentId = row.ApartmentId,
            ApartmentName = row.ApartmentName,
            ApartmentImageUrl = await apartmentImageUrlTask,
            Address = new AddressResponse
            {
                Country = row.Country,
                State = row.State,
                ZipCode = row.ZipCode,
                City = row.City,
                Street = row.Street
            },

            DurationStart = row.DurationStart,
            DurationEnd = row.DurationEnd,
            Nights = nights,

            Currency = row.Currency,
            PricePerNight = nights > 0 ? row.PriceForPeriodAmount / nights : row.PriceForPeriodAmount,
            PriceForPeriodAmount = row.PriceForPeriodAmount,
            CleaningFeeAmount = row.CleaningFeeAmount,
            AmenitiesUpChargeAmount = row.AmenitiesUpChargeAmount,
            TotalPriceAmount = row.TotalPriceAmount,

            Host = new BookingHostResponse
            {
                Id = row.HostId,
                FullName = row.HostFullName,
                AvatarUrl = await hostAvatarUrlTask
            },

            HasReview = row.HasReview,

            ConversationId = row.ConversationId
        };
    }

    internal sealed class BookingRow
    {
        public Guid Id { get; init; }
        public Guid GuestId { get; init; }
        public BookingStatus Status { get; init; }
        public PaymentStatus? PaymentStatus { get; init; }
        public bool HasReview { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public DateTime UpdatedOnUtc { get; init; }

        public Guid ApartmentId { get; init; }
        public string ApartmentName { get; init; } = string.Empty;
        public string? ApartmentImageKey { get; init; }
        public string Country { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string ZipCode { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Street { get; init; } = string.Empty;

        public DateOnly DurationStart { get; init; }
        public DateOnly DurationEnd { get; init; }

        public string Currency { get; init; } = string.Empty;
        public decimal PriceForPeriodAmount { get; init; }
        public decimal CleaningFeeAmount { get; init; }
        public decimal AmenitiesUpChargeAmount { get; init; }
        public decimal TotalPriceAmount { get; init; }

        public Guid HostId { get; init; }
        public string HostFullName { get; init; } = string.Empty;
        public string? HostAvatarKey { get; init; }

        public Guid? ConversationId { get; init; }
    }
}