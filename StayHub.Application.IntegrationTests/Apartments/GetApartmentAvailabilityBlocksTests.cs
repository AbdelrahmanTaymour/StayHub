using FluentAssertions;
using StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentAvailabilityBlocksTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(Guid.CreateVersion7(), null, null));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnBlocksWithoutReason_WhenCallerIsAnonymous()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var block = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 27),
            ApartmentUnavailabilityReason.UnderMaintenance, DateTime.UtcNow);
        DbContext.AddRange(owner, apartment, block);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().ContainSingle();
        result.Value.Blocks[0].Id.Should().Be(block.Id);
        result.Value.Blocks[0].Reason.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnBlocksWithoutReason_WhenCallerIsAnUnrelatedUser()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var block = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 27),
            ApartmentUnavailabilityReason.OwnerBlocked, DateTime.UtcNow);
        DbContext.AddRange(owner, apartment, block);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().ContainSingle();
        result.Value.Blocks[0].Reason.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnBlocksOrderedByStart_WithReason_WhenCallerIsTheOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var laterBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 3),
            ApartmentUnavailabilityReason.OwnerBlocked, DateTime.UtcNow);
        var earlierBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 27),
            ApartmentUnavailabilityReason.UnderMaintenance, DateTime.UtcNow);

        DbContext.AddRange(laterBlock, earlierBlock);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().HaveCount(2);
        result.Value.Blocks[0].Id.Should().Be(earlierBlock.Id);
        result.Value.Blocks[0].Reason.Should().Be("Under Maintenance");
        result.Value.Blocks[1].Id.Should().Be(laterBlock.Id);
        result.Value.Blocks[1].Reason.Should().Be("Owner Blocked");
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnBlocksWithReason_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var block = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 27),
            ApartmentUnavailabilityReason.UnderMaintenance, DateTime.UtcNow);
        DbContext.AddRange(owner, apartment, block);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().ContainSingle(b => b.Id == block.Id);
        result.Value.Blocks[0].Reason.Should().Be("Under Maintenance");
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldFilterByYearAndMonth_WhenProvided()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var octoberBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 27),
            ApartmentUnavailabilityReason.UnderMaintenance, DateTime.UtcNow);
        var novemberBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 3),
            ApartmentUnavailabilityReason.OwnerBlocked, DateTime.UtcNow);

        DbContext.AddRange(octoberBlock, novemberBlock);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, 2026, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().ContainSingle(b => b.Id == octoberBlock.Id);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldIncludeABlockSpanningIntoTheRequestedMonth()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var spanningBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id, new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 3),
            ApartmentUnavailabilityReason.UnderMaintenance, DateTime.UtcNow);
        DbContext.Add(spanningBlock);
        await DbContext.SaveChangesAsync();

        // Act — anonymous
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, 2026, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().ContainSingle(b => b.Id == spanningBlock.Id);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnConfirmedBookings_AsBookedRanges_ForAnonymousCallers()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 18), PricingService);
        booking.Confirm(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        // Act 
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BookedRanges.Should().ContainSingle(r =>
            r.BookingId == booking.Id
            && r.StartDate == new DateOnly(2026, 10, 12)
            && r.EndDate == new DateOnly(2026, 10, 18));
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldNotReturnReservedBookings_AsBookedRanges()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 18), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BookedRanges.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnEmptyLists_WhenApartmentHasNoBlocksOrBookings()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().BeEmpty();
        result.Value.BookedRanges.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnNotFound_WhenApartmentIsInactive_AndCallerIsAnonymous()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnNotFound_WhenApartmentIsInactive_AndCallerIsUnrelatedUser()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnDetails_WhenApartmentIsInactive_AndCallerIsTheOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailabilityBlocks_ShouldReturnDetails_WhenApartmentIsInactive_AndCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        apartment.Deactivate();
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(new GetApartmentAvailabilityBlocksQuery(apartment.Id, null, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}