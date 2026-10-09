using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.GetBookingsByUser;

internal sealed class GetBookingsByUserQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetBookingsByUserQuery, IReadOnlyList<BookingSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<BookingSummaryResponse>>> Handle(
        GetBookingsByUserQuery request,
        CancellationToken cancellationToken)
    {
        // Enforce Self or Admin authorization check
        var isSelf = userContext.UserId == request.UserId;
        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);

        if (!isSelf && !isAdmin)
            return Result.Failure<IReadOnlyList<BookingSummaryResponse>>(BookingErrors.NotAuthorized);

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               b.id AS Id,
                               b.apartment_id AS ApartmentId,
                               b.status AS Status,
                               b.total_price_amount AS TotalPriceAmount,
                               b.total_price_currency AS TotalPriceCurrency,
                               b.duration_start AS DurationStart,
                               b.duration_end AS DurationEnd,
                               
                               p.status AS PaymentStatus
                           FROM bookings b

                           LEFT JOIN LATERAL (
                               SELECT p.status
                               FROM payments p
                               WHERE p.booking_id = b.id
                               ORDER BY p.created_on_utc DESC, p.id DESC
                               LIMIT 1
                           ) p ON TRUE

                           WHERE b.user_id = @UserId
                           ORDER BY b.created_on_utc DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var bookings = await connection.QueryAsync<BookingSummaryResponse>(
            sql,
            new
            {
                request.UserId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return bookings.ToList();
    }
}