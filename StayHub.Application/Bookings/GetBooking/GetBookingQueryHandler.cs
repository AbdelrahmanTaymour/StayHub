using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.GetBooking;

internal sealed class GetBookingQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetBookingQuery, BookingResponse>
{
    public async Task<Result<BookingResponse>> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        // Enforce guest, owner, or admin ownership in query
        const string sql = """
                           SELECT
                               b.id AS Id,
                               b.apartment_id AS ApartmentId,
                               b.user_id AS UserId,
                               b.status AS Status,
                               b.price_for_period_amount AS PriceAmount,
                               b.price_for_period_currency AS PriceCurrency,
                               b.cleaning_fee_amount AS CleaningFeeAmount,
                               b.cleaning_fee_currency AS CleaningFeeCurrency,
                               b.amenities_up_charge_amount AS AmenitiesUpChargeAmount,
                               b.amenities_up_charge_currency AS AmenitiesUpChargeCurrency,
                               b.total_price_amount AS TotalPriceAmount,
                               b.total_price_currency AS TotalPriceCurrency,
                               b.duration_start AS DurationStart,
                               b.duration_end AS DurationEnd,
                               b.created_on_utc AS CreatedOnUtc
                           FROM bookings b
                           INNER JOIN apartments a ON a.id = b.apartment_id
                           WHERE b.id = @BookingId
                             AND (b.user_id = @UserId OR a.owner_id = @UserId OR @IsAdmin = TRUE)
                           """;

        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);

        var bookingResponse = await connection.QueryFirstOrDefaultAsync<BookingResponse>(
            sql,
            new
            {
                request.BookingId,
                userContext.UserId,
                IsAdmin = isAdmin
            });

        return bookingResponse ?? Result.Failure<BookingResponse>(BookingErrors.NotFound);
    }
}