using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Payments.MarkPaymentFailed;
using StayHub.Domain.Payments;

namespace StayHub.Application.IntegrationTests.Payments;

public class MarkPaymentFailedTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task MarkPaymentFailed_ShouldPersistStatusAndEmailGuest_ViaOutboxPipeline()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var payment = PaymentTestData.Initiate(booking.Id, booking.TotalPrice.Amount, booking.TotalPrice.Currency.Code,
            "pi_fails");
        DbContext.Add(payment);
        await DbContext.SaveChangesAsync();

        var command = new MarkPaymentFailedCommand("pi_fails");

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Set<Payment>().SingleAsync(p => p.Id == payment.Id);
        persisted.Status.Should().Be(PaymentStatus.Failed);

        EmailService.SentEmails.Should().ContainSingle(e =>
            e.To.Value == guest.Email.Value && e.Subject == "Payment failed");
    }

    [Fact]
    public async Task MarkPaymentFailed_ShouldReturnNotPending_WhenPaymentAlreadySucceeded()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        booking.Confirm(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var payment = PaymentTestData.Initiate(booking.Id, booking.TotalPrice.Amount, booking.TotalPrice.Currency.Code,
            "pi_already_succeeded");
        payment.MarkAsSucceeded(DateTime.UtcNow);
        DbContext.Add(payment);
        await DbContext.SaveChangesAsync();

        var command = new MarkPaymentFailedCommand("pi_already_succeeded");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotPending);
    }
}