using FluentAssertions;
using StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentAvailabilityBlocksTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnEmptyBlocks_WhenApartmentHasNoAvailabilityBlocks()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentAvailabilityBlocksQuery(
            apartment.Id,
            null,
            null);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnAllBlocks_WhenNoMonthFilterIsProvided()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var firstBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 10, 24),
            new DateOnly(2025, 10, 27),
            ApartmentUnavailabilityReason.UnderMaintenance,
            DateTime.UtcNow);

        var secondBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 11, 1),
            new DateOnly(2025, 11, 3),
            ApartmentUnavailabilityReason.OwnerBlocked,
            DateTime.UtcNow);

        DbContext.AddRange(firstBlock, secondBlock);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentAvailabilityBlocksQuery(
            apartment.Id,
            null,
            null);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().HaveCount(2);

        result.Value.Blocks.Should().SatisfyRespectively(
            first =>
            {
                first.Id.Should().Be(firstBlock.Id);
                first.StartDate.Should().Be(new DateOnly(2025, 10, 24));
                first.EndDate.Should().Be(new DateOnly(2025, 10, 27));
                first.Reason.Should().Be("Under Maintenance");
            },
            second =>
            {
                second.Id.Should().Be(secondBlock.Id);
                second.StartDate.Should().Be(new DateOnly(2025, 11, 1));
                second.EndDate.Should().Be(new DateOnly(2025, 11, 3));
                second.Reason.Should().Be("Owner Blocked");
            });
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnBlocksOverlappingRequestedMonth()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var startsBeforeMonth = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 9, 28),
            new DateOnly(2025, 10, 3),
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        var insideMonth = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 10, 10),
            new DateOnly(2025, 10, 15),
            ApartmentUnavailabilityReason.UnderMaintenance,
            DateTime.UtcNow);

        var endsAfterMonth = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 10, 31),
            new DateOnly(2025, 11, 2),
            ApartmentUnavailabilityReason.OwnerBlocked,
            DateTime.UtcNow);

        var afterMonth = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 11, 5),
            new DateOnly(2025, 11, 8),
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        var beforeMonth = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 9, 1),
            new DateOnly(2025, 9, 10),
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        DbContext.AddRange(
            startsBeforeMonth,
            insideMonth,
            endsAfterMonth,
            afterMonth,
            beforeMonth);

        await DbContext.SaveChangesAsync();

        var query = new GetApartmentAvailabilityBlocksQuery(
            apartment.Id,
            2025,
            10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().HaveCount(3);

        result.Value.Blocks.Select(x => x.Id)
            .Should()
            .BeEquivalentTo([
                startsBeforeMonth.Id,
                insideMonth.Id,
                endsAfterMonth.Id
            ]);
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var query = new GetApartmentAvailabilityBlocksQuery(
            Guid.CreateVersion7(),
            null,
            null);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnOnlyBlocksBelongingToRequestedApartment()
    {
        // Arrange
        var owner = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(owner.Id);
        var otherApartment = ApartmentTestData.CreateApartment(owner.Id);

        DbContext.AddRange(owner, apartment, otherApartment);
        await DbContext.SaveChangesAsync();

        var apartmentBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 10, 10),
            new DateOnly(2025, 10, 15),
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        var otherApartmentBlock = ApartmentAvailabilityBlock.Create(
            otherApartment.Id,
            new DateOnly(2025, 10, 10),
            new DateOnly(2025, 10, 15),
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        DbContext.AddRange(apartmentBlock, otherApartmentBlock);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentAvailabilityBlocksQuery(
            apartment.Id,
            2025,
            10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Should().ContainSingle();
        result.Value.Blocks[0].Id.Should().Be(apartmentBlock.Id);
    }

    [Fact]
    public async Task GetApartmentAvailability_ShouldReturnBlocksOrderedByStartDate()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(owner.Id);

        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var laterBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 10, 20),
            new DateOnly(2025, 10, 25),
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        var earlierBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            new DateOnly(2025, 10, 5),
            new DateOnly(2025, 10, 10),
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        DbContext.AddRange(laterBlock, earlierBlock);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentAvailabilityBlocksQuery(
            apartment.Id,
            null,
            null);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Blocks.Select(x => x.Id)
            .Should()
            .ContainInOrder(
                earlierBlock.Id,
                laterBlock.Id);
    }
}