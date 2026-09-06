using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Application.Payments.GetPaymentByBooking;
using StayHub.Domain.Payments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Payments;

public class GetPaymentByBookingTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetPaymentByBooking_ShouldReturnNotFound_WhenNoPaymentExists()
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
        var result = await Sender.Send(new GetPaymentByBookingQuery(booking.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotFound);
    }

    [Fact]
    public async Task GetPaymentByBooking_ShouldReturnCorrectAmount_WhenCallerIsTheGuest()
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

        var payment = PaymentTestData.Initiate(booking.Id, 725.50m, "USD", "pi_test_abc123");
        DbContext.Add(payment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetPaymentByBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AmountValue.Should().Be(725.50m);
        result.Value.AmountCurrency.Should().Be("USD");
        result.Value.Status.Should().Be((int)PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetPaymentByBooking_ShouldReturnNotFound_WhenCallerIsUnrelatedNonAdmin()
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

        var payment = PaymentTestData.Initiate(booking.Id, 500m, "USD", "pi_test_def456");
        DbContext.Add(payment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetPaymentByBookingQuery(booking.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.NotFound);
    }
}