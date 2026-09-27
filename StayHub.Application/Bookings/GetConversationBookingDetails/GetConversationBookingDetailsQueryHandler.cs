using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;

namespace StayHub.Application.Bookings.GetConversationBookingDetails;

internal sealed class GetConversationBookingDetailsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
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
                               img.url AS ApartmentImageUrl,
                               a.address_state || ', ' || a.address_city || ' ' || a.address_zip_code AS ApartmentAddress,
                               b.duration_start AS CheckIn,
                               b.duration_end AS CheckOut,
                               b.total_price_amount AS TotalPriceAmount,
                               b.total_price_currency AS TotalPriceCurrency,
                               owner.first_name || ' ' || owner.last_name AS HostName,
                               owner_profile.avatar_url AS HostAvatarUrl,
                               owner_profile.phone_number AS HostPhoneNumber

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

                           WHERE c.id = @ConversationId
                             AND (c.guest_id = @UserId OR c.owner_id = @UserId)
                           """;

        var row = await connection.QueryFirstOrDefaultAsync<ReservationRow>(
            sql, new { request.ConversationId, userContext.UserId });

        if (row is null)
        {
            return Result.Failure<ConversationBookingDetailsResponse>(BookingErrors.NotFound);
        }

        return new ConversationBookingDetailsResponse
        {
            BookingId = row.BookingId,
            Status = row.Status,
            ApartmentId = row.ApartmentId,
            ApartmentName = row.ApartmentName,
            ApartmentImageUrl = row.ApartmentImageUrl,
            ApartmentAddress = row.ApartmentAddress,
            CheckIn = row.CheckIn,
            CheckOut = row.CheckOut,
            Nights = row.CheckOut.DayNumber - row.CheckIn.DayNumber,
            TotalPriceAmount = row.TotalPriceAmount,
            TotalPriceCurrency = row.TotalPriceCurrency,
            HostName = row.HostName,
            HostAvatarUrl = row.HostAvatarUrl,
            HostPhoneNumber = row.HostPhoneNumber
        };
    }

    private sealed class ReservationRow
    {
        public Guid BookingId { get; init; }
        public BookingStatus Status { get; init; }
        public Guid ApartmentId { get; init; }
        public string ApartmentName { get; init; } = string.Empty;
        public string? ApartmentImageUrl { get; init; }
        public string ApartmentAddress { get; init; } = string.Empty;
        public DateOnly CheckIn { get; init; }
        public DateOnly CheckOut { get; init; }
        public decimal TotalPriceAmount { get; init; }
        public string TotalPriceCurrency { get; init; } = string.Empty;
        public string HostName { get; init; } = string.Empty;
        public string? HostAvatarUrl { get; init; }
        public string? HostPhoneNumber { get; init; }
    }
}