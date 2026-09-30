using FluentAssertions;
using StayHub.Application.Bookings.GetApartmentBookings;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class GetApartmentBookingsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetApartmentBookings_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(Guid.CreateVersion7()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnNotAuthorized_WhenCallerIsNotTheOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnEmptyPage_WhenApartmentHasNoBookings()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnGuestDetailsAndDerivedFields()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser(firstName: "David", lastName: "Kim");
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 28), new DateOnly(2026, 11, 2), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.Should().ContainSingle().Which;
        item.GuestId.Should().Be(guest.Id);
        item.GuestFullName.Should().Be("David Kim");
        item.GuestAvatarUrl.Should().BeNull();
        item.Status.Should().Be(BookingStatus.Reserved);
        item.Nights.Should().Be(5);
        item.DurationStart.Should().Be(new DateOnly(2026, 10, 28));
        item.DurationEnd.Should().Be(new DateOnly(2026, 11, 2));
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnGuestAvatar_WhenGuestHasProfile()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var profile = UserTestData.CreateProfile(guest.Id);
        profile.UpdateAvatar(new AvatarUrl("https://test-storage.local/guest-avatar.png"), DateTime.UtcNow);
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, profile, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 28), new DateOnly(2026, 11, 2), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].GuestAvatarUrl.Should().Be("https://test-storage.local/guest-avatar.png");
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldNotReturnAnotherApartmentsBookings()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment A");
        var otherApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment B");
        DbContext.AddRange(owner, guest, apartment, otherApartment);
        await DbContext.SaveChangesAsync();

        var bookingOnOtherApartment = BookingTestData.Reserve(
            otherApartment, guest.Id, new DateOnly(2026, 10, 28), new DateOnly(2026, 11, 2), PricingService);
        DbContext.Add(bookingOnOtherApartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnBookings_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 28), new DateOnly(2026, 11, 2), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == booking.Id);
    }

    // ---------- Filtering ----------

    private async Task<(Guid OwnerId, Guid ApartmentId, Booking Pending, Booking Confirmed, Booking Cancelled,
        Booking Rejected)> ArrangeOneBookingPerStatusAsync()
    {
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var pending = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5), PricingService, baseTime);

        var confirmed = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 18), PricingService,
            baseTime.AddMinutes(1));
        confirmed.Confirm(baseTime.AddMinutes(1));

        var cancelled = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService,
            baseTime.AddMinutes(2));
        cancelled.Cancel(baseTime.AddMinutes(2));

        var rejected = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), PricingService,
            baseTime.AddMinutes(3));
        rejected.Reject(owner.Id, baseTime.AddMinutes(3));

        DbContext.AddRange(pending, confirmed, cancelled, rejected);
        await DbContext.SaveChangesAsync();

        return (owner.Id, apartment.Id, pending, confirmed, cancelled, rejected);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldReturnAllStatuses_WhenFilterIsAll()
    {
        // Arrange
        var (ownerId, apartmentId, _, _, _, _) = await ArrangeOneBookingPerStatusAsync();
        SetCurrentUser(ownerId, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartmentId, ApartmentBookingsFilter.All));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldOnlyReturnPending_WhenFilterIsPending()
    {
        // Arrange
        var (ownerId, apartmentId, pending, _, _, _) = await ArrangeOneBookingPerStatusAsync();
        SetCurrentUser(ownerId, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartmentId, ApartmentBookingsFilter.Pending));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == pending.Id);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldOnlyReturnConfirmed_WhenFilterIsConfirmed()
    {
        // Arrange
        var (ownerId, apartmentId, _, confirmed, _, _) = await ArrangeOneBookingPerStatusAsync();
        SetCurrentUser(ownerId, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartmentId, ApartmentBookingsFilter.Confirmed));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == confirmed.Id);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldOnlyReturnCancelled_WhenFilterIsCancelled()
    {
        // Arrange
        var (ownerId, apartmentId, _, _, cancelled, _) = await ArrangeOneBookingPerStatusAsync();
        SetCurrentUser(ownerId, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartmentId, ApartmentBookingsFilter.Cancelled));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == cancelled.Id);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldOnlyReturnRejected_WhenFilterIsRejected()
    {
        // Arrange
        var (ownerId, apartmentId, _, _, _, rejected) = await ArrangeOneBookingPerStatusAsync();
        SetCurrentUser(ownerId, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartmentId, ApartmentBookingsFilter.Rejected));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == rejected.Id);
    }

    // ---------- Search ----------

    [Fact]
    public async Task GetApartmentBookings_ShouldFilterByGuestName_CaseInsensitively()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var matchingGuest = UserTestData.CreateUser(firstName: "David", lastName: "Kim");
        var otherGuest = UserTestData.CreateUser(firstName: "Elena", lastName: "Rostova");
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, matchingGuest, otherGuest, apartment);
        await DbContext.SaveChangesAsync();

        var matchingBooking = BookingTestData.Reserve(
            apartment, matchingGuest.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5), PricingService);
        var otherBooking = BookingTestData.Reserve(
            apartment, otherGuest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.AddRange(matchingBooking, otherBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id, Search: "david"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == matchingBooking.Id);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldFilterByBookingId()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var matchingBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5), PricingService);
        var otherBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.AddRange(matchingBooking, otherBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentBookingsQuery(apartment.Id, Search: matchingBooking.Id.ToString()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == matchingBooking.Id);
    }

    // ---------- Sorting ----------

    [Fact]
    public async Task GetApartmentBookings_ShouldSortByCheckInAscending_ByDefault()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var later = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), PricingService);
        var earlier = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5), PricingService);
        DbContext.AddRange(later, earlier);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Id.Should().Be(earlier.Id);
        result.Value.Items[1].Id.Should().Be(later.Id);
    }

    [Fact]
    public async Task GetApartmentBookings_ShouldSortByTotalDescending_WhenRequested()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, priceAmount: 100m, priceCurrency: "USD");
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        // Same price/night; different lengths give different totals.
        var cheaper = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3), PricingService); // 2 nights
        var pricier = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 8), PricingService); // 7 nights
        DbContext.AddRange(cheaper, pricier);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetApartmentBookingsQuery(apartment.Id, Sort: ApartmentBookingsSort.TotalDesc));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Id.Should().Be(pricier.Id);
        result.Value.Items[1].Id.Should().Be(cheaper.Id);
    }

    // ---------- Pagination ----------

    [Fact]
    public async Task GetApartmentBookings_ShouldRespectPagination()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var bookings = Enumerable.Range(0, 3)
            .Select(i => BookingTestData.Reserve(
                apartment, guest.Id,
                new DateOnly(2026, 10, 1).AddDays(i * 10), new DateOnly(2026, 10, 5).AddDays(i * 10),
                PricingService))
            .ToList();
        DbContext.AddRange(bookings);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentBookingsQuery(apartment.Id, Page: 2, PageSize: 2));

        // Assert — sorted check-in ascending by default, page size 2: page 2 holds the 3rd/oldest-checkin one.
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == bookings[2].Id);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
    }
}