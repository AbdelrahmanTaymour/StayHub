using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.GetBookingsByUser;

public class GetBookingsByUserQueryHandler(
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
                               id AS Id,
                               apartment_id AS ApartmentId,
                               status AS Status,
                               total_price_amount AS TotalPriceAmount,
                               total_price_currency AS TotalPriceCurrency,
                               duration_start AS DurationStart,
                               duration_end AS DurationEnd
                           FROM bookings
                           WHERE user_id = @UserId
                           ORDER BY created_on_utc DESC
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