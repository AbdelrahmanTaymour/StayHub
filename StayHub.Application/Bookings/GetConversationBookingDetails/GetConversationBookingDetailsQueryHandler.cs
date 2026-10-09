using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;

namespace StayHub.Application.Bookings.GetConversationBookingDetails;

internal sealed class GetConversationBookingDetailsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetConversationBookingDetailsQuery, ConversationBookingDetailsResponse>
{
    public async Task<Result<ConversationBookingDetailsResponse>> Handle(
        GetConversationBookingDetailsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               b.id AS BookingId,
                               b.status AS Status,
                               a.id AS ApartmentId,
                               a.name AS ApartmentName,
                               img.key AS ApartmentImageKey,
                               a.address_state || ', ' || a.address_city || ' ' || a.address_zip_code AS ApartmentAddress,
                               b.duration_start AS CheckIn,
                               b.duration_end AS CheckOut,
                               b.total_price_amount AS TotalPriceAmount,
                               b.total_price_currency AS TotalPriceCurrency,
                               owner.first_name || ' ' || owner.last_name AS HostName,
                               owner_profile.avatar_key AS HostAvatarKey,
                               owner_profile.phone_number AS HostPhoneNumber,
                               p.status AS PaymentStatus

                           FROM conversations c

                           JOIN bookings b
                               ON b.id = c.booking_id

                           JOIN apartments a
                               ON a.id = b.apartment_id

                           JOIN users owner
                               ON owner.id = c.owner_id

                           LEFT JOIN user_profiles owner_profile
                               ON owner_profile.user_id = owner.id

                           LEFT JOIN apartment_images img
                               ON img.apartment_id = a.id
                               AND img.is_primary = true

                           LEFT JOIN LATERAL (
                               SELECT p.status
                               FROM payments p
                               WHERE p.booking_id = b.id
                               ORDER BY p.created_on_utc DESC, p.id DESC
                               LIMIT 1
                           ) p ON TRUE

                           WHERE c.id = @ConversationId
                             AND (c.guest_id = @UserId OR c.owner_id = @UserId)
                           """;

        var row = await connection.QueryFirstOrDefaultAsync<ReservationRow>(
            sql, new { request.ConversationId, userContext.UserId });

        if (row is null)
        {
            return Result.Failure<ConversationBookingDetailsResponse>(BookingErrors.NotFound);
        }

        return await ToConversationBookingDetailsResponseAsync(row, cancellationToken);
    }

    internal async Task<ConversationBookingDetailsResponse> ToConversationBookingDetailsResponseAsync(
        ReservationRow row,
        CancellationToken cancellationToken)
    {
        var apartmentImageUrlTask = string.IsNullOrWhiteSpace(row.ApartmentImageKey)
            ? Task.FromResult<string?>(null)
            : fileStorageService.GeneratePresignedUrlAsync(row.ApartmentImageKey, cancellationToken)!;

        var hostAvatarUrlTask = string.IsNullOrWhiteSpace(row.HostAvatarKey)
            ? Task.FromResult<string?>(null)
            : fileStorageService.GeneratePresignedUrlAsync(row.HostAvatarKey, cancellationToken)!;

        await Task.WhenAll(apartmentImageUrlTask, hostAvatarUrlTask);

        return new ConversationBookingDetailsResponse
        {
            BookingId = row.BookingId,
            Status = row.Status,
            PaymentStatus = row.PaymentStatus,
            ApartmentId = row.ApartmentId,
            ApartmentName = row.ApartmentName,
            ApartmentImageUrl = await apartmentImageUrlTask,
            ApartmentAddress = row.ApartmentAddress,
            CheckIn = row.CheckIn,
            CheckOut = row.CheckOut,
            Nights = row.CheckOut.DayNumber - row.CheckIn.DayNumber,
            TotalPriceAmount = row.TotalPriceAmount,
            TotalPriceCurrency = row.TotalPriceCurrency,
            HostName = row.HostName,
            HostAvatarUrl = await hostAvatarUrlTask,
            HostPhoneNumber = row.HostPhoneNumber
        };
    }

    internal sealed class ReservationRow
    {
        public Guid BookingId { get; init; }
        public BookingStatus Status { get; init; }
        public PaymentStatus? PaymentStatus { get; init; }
        public Guid ApartmentId { get; init; }
        public string ApartmentName { get; init; } = string.Empty;
        public string? ApartmentImageKey { get; init; }
        public string ApartmentAddress { get; init; } = string.Empty;
        public DateOnly CheckIn { get; init; }
        public DateOnly CheckOut { get; init; }
        public decimal TotalPriceAmount { get; init; }
        public string TotalPriceCurrency { get; init; } = string.Empty;
        public string HostName { get; init; } = string.Empty;
        public string? HostAvatarKey { get; init; }
        public string? HostPhoneNumber { get; init; }
    }
}