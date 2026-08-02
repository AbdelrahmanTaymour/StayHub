using Microsoft.EntityFrameworkCore;
using StayHub.Domain.Payments;

namespace StayHub.Infrastructure.Repositories;

internal sealed class PaymentRepository(ApplicationDbContext dbContext)
    : Repository<Payment>(dbContext), IPaymentRepository
{
    public async Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Payment>()
            .FirstOrDefaultAsync(payment => payment.BookingId == bookingId, cancellationToken);
    }

    public async Task<Payment?> GetActiveByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Payment>()
            .FirstOrDefaultAsync(
                payment =>
                    payment.BookingId == bookingId &&
                    (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Succeeded),
                cancellationToken);
    }

    public async Task<Payment?> GetByProviderReferenceAsync(
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<Payment>()
            .FirstOrDefaultAsync(payment => payment.ProviderReference == providerReference, cancellationToken);
    }
}