using FluentAssertions;
using StayHub.Application.Apartments.GetMyApartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetMyApartmentsTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetMyApartments_ShouldReturnOnlyApartmentsOwnedByCurrentUser()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var otherOwner = UserTestData.CreateUser();

        var ownerApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "My Apartment");

        var otherApartment = ApartmentTestData.CreateApartment(
            ownerId: otherOwner.Id,
            name: "Other Apartment");

        DbContext.AddRange(owner, otherOwner, ownerApartment, otherApartment);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetMyApartmentsQuery(
            OwnerId: owner.Id,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(a => a.Id == ownerApartment.Id);
        result.Value.Items.Should().NotContain(a => a.Id == otherApartment.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyApartments_ShouldReturnOnlyActiveApartments_WhenStatusIsActive()
    {
        // Arrange
        var owner = UserTestData.CreateUser();

        var activeApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Active Apartment");

        var inactiveApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Inactive Apartment");

        inactiveApartment.Deactivate();

        DbContext.AddRange(owner, activeApartment, inactiveApartment);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetMyApartmentsQuery(
            OwnerId: owner.Id,
            Status: MyApartmentsFilter.Active,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(a => a.Id == activeApartment.Id);
        result.Value.Items.Should().NotContain(a => a.Id == inactiveApartment.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyApartments_ShouldReturnOnlyInactiveApartments_WhenStatusIsInactive()
    {
        // Arrange
        var owner = UserTestData.CreateUser();

        var activeApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Active Apartment");

        var inactiveApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Inactive Apartment");

        inactiveApartment.Deactivate();

        DbContext.AddRange(owner, activeApartment, inactiveApartment);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetMyApartmentsQuery(
            OwnerId: owner.Id,
            Status: MyApartmentsFilter.Inactive,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(a => a.Id == inactiveApartment.Id);
        result.Value.Items.Should().NotContain(a => a.Id == activeApartment.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyApartments_ShouldFilterBySearchTerm()
    {
        // Arrange
        var owner = UserTestData.CreateUser();

        var cairoApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Cairo Downtown",
            city: "Cairo");

        var alexandriaApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Alexandria Beach",
            city: "Alexandria");

        DbContext.AddRange(owner, cairoApartment, alexandriaApartment);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetMyApartmentsQuery(
            OwnerId: owner.Id,
            Search: "cairo",
            Page: 1,
            PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(a => a.Id == cairoApartment.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyApartments_ShouldRespectPagination()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var baseTime = DateTime.UtcNow;

        var apartments = Enumerable.Range(0, 3)
            .Select(i =>
                ApartmentTestData.CreateApartment(
                    ownerId: owner.Id,
                    name: $"Paged {i}",
                    utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.Add(owner);
        DbContext.AddRange(apartments);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetMyApartmentsQuery(
            OwnerId: owner.Id,
            Page: 2,
            PageSize: 2);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);

        // created_on_utc DESC means the oldest apartment is on page 2.
        result.Value.Items[0].Id.Should().Be(apartments[0].Id);
    }
}