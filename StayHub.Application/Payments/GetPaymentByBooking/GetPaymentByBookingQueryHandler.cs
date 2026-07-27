using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Payments;

namespace StayHub.Application.Payments.GetPaymentByBooking;

internal sealed class GetPaymentByBookingQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetPaymentByBookingQuery, PaymentResponse>
{
    public async Task<Result<PaymentResponse>> Handle(
        GetPaymentByBookingQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               id AS Id,
                               booking_id AS BookingId,
                               amount_value AS AmountValue,
                               amount_currency AS AmountCurrency,
                               status AS Status,
                               created_on_utc AS CreatedOnUtc,
                               processed_on_utc AS ProcessedOnUtc
                           FROM payments
                           WHERE booking_id = @BookingId
                           """;

        var payment = await connection.QueryFirstOrDefaultAsync<PaymentResponse>(
            sql,
            new { request.BookingId });

        return payment ?? Result.Failure<PaymentResponse>(PaymentErrors.NotFound);
    }
}