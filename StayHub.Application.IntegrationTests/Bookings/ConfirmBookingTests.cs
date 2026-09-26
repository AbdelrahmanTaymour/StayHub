using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Bookings.ConfirmBooking;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class ConfirmBookingTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ConfirmBooking_ShouldPersistBookingStatusAndApartmentLastBooked_Together_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new ConfirmBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert 
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedBooking = await DbContext.Set<Booking>().SingleAsync(b => b.Id == booking.Id);
        persistedBooking.Status.Should().Be(BookingStatus.Confirmed);

        var persistedApartment = await DbContext.Set<Apartment>().SingleAsync(a => a.Id == apartment.Id);
        persistedApartment.LastBookedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmBooking_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        // The guest themselves is not authorized to confirm their own booking
        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new ConfirmBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotAuthorized);
    }

    [Fact]
    public async Task ConfirmBooking_ShouldSendConfirmationEmailToGuest_ViaOutboxPipeline()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new ConfirmBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        EmailService.SentEmails.Should().ContainSingle(e =>
            e.To.Value == guest.Email.Value &&
            e.Subject == "Booking confirmed!");
    }

    [Fact]
    public async Task ConfirmBooking_ShouldPersistBookingAndAvailabilityBlockTogether_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id);

        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment,
            guest.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 5),
            PricingService);

        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new ConfirmBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();

        var persistedBooking = await DbContext
            .Set<Booking>()
            .SingleAsync(b => b.Id == booking.Id);

        persistedBooking.Status.Should().Be(BookingStatus.Confirmed);

        var persistedApartment = await DbContext
            .Set<Apartment>()
            .SingleAsync(a => a.Id == apartment.Id);

        persistedApartment.LastBookedOnUtc.Should().NotBeNull();

        var availabilityBlock = await DbContext
            .Set<ApartmentAvailabilityBlock>()
            .SingleAsync(b =>
                b.ApartmentId == apartment.Id &&
                b.Start == booking.Duration.Start &&
                b.End == booking.Duration.End);

        availabilityBlock.Reason
            .Should()
            .Be(ApartmentUnavailabilityReason.Booked);
    }

    [Fact]
    public async Task ConfirmBooking_ShouldReturnOverlap_WhenAnotherConfirmedBookingCoversTheSameDates()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var firstGuest = UserTestData.CreateUser();
        var secondGuest = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(
            owner,
            firstGuest,
            secondGuest,
            apartment);

        await DbContext.SaveChangesAsync();

        var confirmedBooking = BookingTestData.ReserveAndConfirm(
            apartment,
            firstGuest.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 10),
            PricingService);

        var pendingBooking = BookingTestData.Reserve(
            apartment,
            secondGuest.Id,
            new DateOnly(2026, 11, 5),
            new DateOnly(2026, 11, 8),
            PricingService);

        DbContext.AddRange(confirmedBooking, pendingBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new ConfirmBookingCommand(pendingBooking.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.Overlap);

        DbContext.ChangeTracker.Clear();

        var persistedBooking = await DbContext
            .Set<Booking>()
            .SingleAsync(b => b.Id == pendingBooking.Id);

        persistedBooking.Status.Should().Be(BookingStatus.Reserved);
    }

    [Fact]
    public async Task ConfirmBooking_ShouldConfirm_WhenOtherOverlappingBookingIsStillReserved()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var firstGuest = UserTestData.CreateUser();
        var secondGuest = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, firstGuest, secondGuest, apartment);
        await DbContext.SaveChangesAsync();

        var firstBooking = BookingTestData.Reserve(
            apartment,
            firstGuest.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 10),
            PricingService);

        var secondBooking = BookingTestData.Reserve(
            apartment,
            secondGuest.Id,
            new DateOnly(2026, 11, 5),
            new DateOnly(2026, 11, 8),
            PricingService);

        DbContext.AddRange(firstBooking, secondBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new ConfirmBookingCommand(secondBooking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();

        var persistedFirstBooking = await DbContext
            .Set<Booking>()
            .SingleAsync(b => b.Id == firstBooking.Id);

        var persistedSecondBooking = await DbContext
            .Set<Booking>()
            .SingleAsync(b => b.Id == secondBooking.Id);

        persistedFirstBooking.Status.Should().Be(BookingStatus.Reserved);
        persistedSecondBooking.Status.Should().Be(BookingStatus.Confirmed);
    }
}