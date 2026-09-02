using FluentAssertions;
using StayHub.Application.Bookings.GetBookingsByApartment;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class GetBookingsByApartmentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetBookingsByApartment_ShouldReturnEmpty_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange — unrelated caller gets an empty list, not an error.
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingsByApartmentQuery(apartment.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBookingsByApartment_ShouldReturnBookings_OrderedByDurationStartDescending_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var earlierBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        var laterBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5), PricingService);

        DbContext.AddRange(earlierBooking, laterBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingsByApartmentQuery(apartment.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(laterBooking.Id);
        result.Value[1].Id.Should().Be(earlierBooking.Id);
    }
}