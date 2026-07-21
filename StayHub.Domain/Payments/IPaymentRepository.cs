namespace StayHub.Domain.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    void Add(Payment payment);
}