namespace StayHub.Domain.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns the currently active (Pending or Succeeded) payment for a booking, if any - used to
    ///     decide whether a new payment attempt is allowed. A Failed or Refunded payment does NOT count
    ///     as active, so a guest whose card was declined can retry.
    /// </summary>
    Task<Payment?> GetActiveByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);


    Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken = default);

    void Add(Payment payment);
}