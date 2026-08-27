using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Payments;
using StayHub.Domain.Users;

namespace StayHub.Application.Payments.GetPaymentByBooking;

internal sealed class GetPaymentByBookingQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetPaymentByBookingQuery, PaymentResponse>
{
    public async Task<Result<PaymentResponse>> Handle(
        GetPaymentByBookingQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               p.id AS Id,
                               p.booking_id AS BookingId,
                               p.amount_value AS AmountValue,
                               p.amount_currency AS AmountCurrency,
                               p.status AS Status,
                               p.created_on_utc AS CreatedOnUtc,
                               p.processed_on_utc AS ProcessedOnUtc
                           FROM payments p
                           INNER JOIN bookings b ON b.id = p.booking_id
                           INNER JOIN apartments a ON a.id = b.apartment_id
                           WHERE p.booking_id = @BookingId
                             AND (b.user_id = @UserId OR a.owner_id = @UserId OR @IsAdmin = TRUE)
                           """;

        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);

        var payment = await connection.QueryFirstOrDefaultAsync<PaymentResponse>(
            sql,
            new
            {
                request.BookingId,
                userContext.UserId,
                IsAdmin = isAdmin
            });

        return payment ?? Result.Failure<PaymentResponse>(PaymentErrors.NotFound);
    }
}