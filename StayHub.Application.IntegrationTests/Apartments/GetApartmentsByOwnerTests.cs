using FluentAssertions;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentsByOwnerTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetApartmentsByOwner_ShouldReturnEmptyList_WhenOwnerHasNoApartments()
    {
        // Arrange
        var query = new GetApartmentsByOwnerQuery(Guid.CreateVersion7(), Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldReturnOnlyThatOwnersApartments_WithMappedPrice()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var otherOwner = UserTestData.CreateUser();

        var ownedApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Owned Apartment",
            priceAmount: 300m,
            priceCurrency: "USD");

        var otherOwnersApartment = ApartmentTestData.CreateApartment(
            ownerId: otherOwner.Id,
            name: "Someone Else's Apartment");

        DbContext.AddRange(owner, otherOwner, ownedApartment, otherOwnersApartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, Page: 1, PageSize: 10);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(ownedApartment.Id);
        result.Value[0].Price.Should().Be(300m);
        result.Value[0].Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetApartmentsByOwner_ShouldRespectPagination()
    {
        // Arrange
        var owner = UserTestData.CreateUser();

        var baseTime = DateTime.UtcNow;

        var apartments = Enumerable.Range(0, 3)
            .Select(i => ApartmentTestData.CreateApartment(
                ownerId: owner.Id,
                name: $"Apartment {i}",
                utcNow: baseTime.AddMinutes(i)))
            .ToList();

        DbContext.Add(owner);
        DbContext.AddRange(apartments);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, Page: 1, PageSize: 2);

        // Act
        var result = await Sender.Send(query);

        // Assert — ORDER BY created_on_utc DESC, so most recently created first.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(apartments[2].Id);
        result.Value[1].Id.Should().Be(apartments[1].Id);
    }

    // --- CACHE TESTS ---

    [Fact]
    public async Task GetApartmentsByOwner_ShouldServeSecondCallFromRealRedisCache_NotFromDatabase()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Cached Apartment");
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentsByOwnerQuery(owner.Id, Page: 1, PageSize: 10);

        // Act — first call: cache miss, hits the database, then populates Redis.
        var firstResult = await Sender.Send(query);
        firstResult.IsSuccess.Should().BeTrue();
        firstResult.Value.Should().ContainSingle(a => a.Id == apartment.Id);

        // Prove the value actually landed in real Redis
        var cachedValue = await CacheService.GetAsync<IReadOnlyList<ApartmentSummaryResponse>>(query.CacheKey);
        cachedValue.Should().NotBeNull();

        // Remove the apartment directly from Postgres, bypassing the
        // repository/cache-invalidation path entirely — if the second Send
        // still returns it, the result came from Redis, not a fresh query.
        DbContext.Remove(apartment);
        await DbContext.SaveChangesAsync();

        // Second call
        var secondResult = await Sender.Send(query);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().ContainSingle(a => a.Id == apartment.Id);
    }
}