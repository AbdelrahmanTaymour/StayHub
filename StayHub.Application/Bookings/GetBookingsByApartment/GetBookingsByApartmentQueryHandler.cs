using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Bookings.GetBookingsByUser;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Bookings.GetBookingsByApartment;

public class GetBookingsByApartmentQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetBookingsByApartmentQuery, IReadOnlyList<BookingSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<BookingSummaryResponse>>> Handle(
        GetBookingsByApartmentQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               id AS Id,
                               apartment_id AS ApartmentId,
                               status AS Status,
                               total_price_amount AS TotalPriceAmount,
                               total_price_currency AS TotalPriceCurrency,
                               duration_start AS DurationStart,
                               duration_end AS DurationEnd
                           FROM bookings
                           WHERE apartment_id = @ApartmentId
                           ORDER BY duration_start DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var bookings = await connection.QueryAsync<BookingSummaryResponse>(
            sql,
            new
            {
                request.ApartmentId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return bookings.ToList();
    }
}