using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Payments.InitiatePayment;
using StayHub.Domain.Payments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Payments;

public class InitiatePaymentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task InitiatePayment_ShouldPersistPaymentAndReturnClientSecret_WhenBookingIsConfirmed()
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

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new InitiatePaymentCommand(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClientSecret.Should().NotBeNullOrWhiteSpace();

        PaymentGatewayService.CreatedPaymentIntents.Should().ContainSingle(i => i.Amount == booking.TotalPrice.Amount);

        DbContext.ChangeTracker.Clear();
        var persistedPayment = await DbContext.Set<Payment>().SingleAsync(p => p.Id == result.Value.PaymentId);
        persistedPayment.BookingId.Should().Be(booking.Id);
        persistedPayment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task InitiatePayment_ShouldReturnBookingNotConfirmed_WhenBookingIsStillReserved()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new InitiatePaymentCommand(booking.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.BookingNotConfirmed);
    }

    [Fact]
    public async Task InitiatePayment_ShouldReturnAlreadyInitiated_WhenAnActivePaymentAlreadyExists()
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

        var existingPayment = PaymentTestData.Initiate(booking.Id, booking.TotalPrice.Amount,
            booking.TotalPrice.Currency.Code, "pi_existing");
        DbContext.Add(existingPayment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new InitiatePaymentCommand(booking.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.AlreadyInitiated);
    }

    [Fact]
    public async Task InitiatePayment_ShouldRefundTheCreatedIntent_WhenDatabaseSaveFailsAfterRealIntentCreation()
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

        SetCurrentUser(guest.Id, Role.Guest.Name);

        SaveChangesInterceptor.FailNextSave = new InvalidOperationException("Simulated database failure.");

        // Act
        var act = async () => await Sender.Send(new InitiatePaymentCommand(booking.Id));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        var createdIntent = PaymentGatewayService.CreatedPaymentIntents.Should().ContainSingle().Subject;
        PaymentGatewayService.IssuedRefunds.Should()
            .ContainSingle(r => r.ProviderReference.Value == createdIntent.ProviderReference);
    }
}