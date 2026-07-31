using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StayHub.Domain.Bookings;
using StayHub.Domain.Payments;
using StayHub.Domain.Shared;

namespace StayHub.Infrastructure.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);

        builder.OwnsOne(payment => payment.Amount, amountBuilder =>
        {
            amountBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.Property(payment => payment.ProviderReference)
            .HasMaxLength(200)
            .HasConversion(payment => payment.Value, payment => new ProviderReference(payment));

        // Partial unique index: only ONE active (Pending or Succeeded) payment per booking at a time.
        // A Failed payment must NOT block a retry, or a guest whose card gets declined would be
        // permanently unable to ever pay for that booking - PaymentStatus: Pending=1, Succeeded=2.
        builder.HasIndex(payment => payment.BookingId)
            .IsUnique()
            .HasFilter("status IN (1, 2)");

        builder.HasIndex(payment => payment.ProviderReference).IsUnique();

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(payment => payment.BookingId);

        builder.Property<uint>("Version").IsRowVersion();
    }
}