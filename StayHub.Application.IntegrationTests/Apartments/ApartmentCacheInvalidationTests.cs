using FluentAssertions;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Apartments.DeactivateApartment;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

// This deliberately tests only ONE of the seven events
// ApartmentCacheInvalidationHandler reacts to (via DeactivateApartment) to
// prove the full real pipeline once: Command -> SaveChanges -> OutboxMessage
// -> ProcessOutboxMessagesJob -> MediatR Publish -> ApartmentCacheInvalidationHandler
// -> real Redis.
public class ApartmentCacheInvalidationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task DeactivateApartment_ShouldInvalidateRealRedisCacheEntry_ViaOutboxPipeline()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var cacheKey = CacheKeys.Apartment(apartment.Id);
        await CacheService.SetAsync(cacheKey, "cached-value");

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new DeactivateApartmentCommand(apartment.Id);

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        var cachedValueAfterProcessing = await CacheService.GetAsync<string>(cacheKey, CancellationToken.None);
        cachedValueAfterProcessing.Should().BeNull();
    }
}