using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Payments.RefundPayment;
using StayHub.Domain.Payments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Payments;

public class RefundPaymentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task RefundPayment_ShouldIssueRealRefundAndEmailGuest_ViaOutboxPipeline()
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
            "pi_to_refund");
        payment.MarkAsSucceeded(DateTime.UtcNow);
        DbContext.Add(payment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new RefundPaymentCommand(payment.Id);

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        PaymentGatewayService.IssuedRefunds.Should().ContainSingle(r => r.ProviderReference.Value == "pi_to_refund");

        DbContext.ChangeTracker.Clear();
        var persistedPayment = await DbContext.Set<Payment>().SingleAsync(p => p.Id == payment.Id);
        persistedPayment.Status.Should().Be(PaymentStatus.Refunded);

        EmailService.SentEmails.Should().ContainSingle(e =>
            e.To.Value == guest.Email.Value && e.Subject == "Payment refunded");
    }

    [Fact]
    public async Task RefundPayment_ShouldReturnNotAuthorized_WhenCallerIsNotGuestOwnerOrAdmin()
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
            "pi_no_refund");
        payment.MarkAsSucceeded(DateTime.UtcNow);
        DbContext.Add(payment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var command = new RefundPaymentCommand(payment.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotAuthorized);

        PaymentGatewayService.IssuedRefunds.Should().BeEmpty();
    }
}