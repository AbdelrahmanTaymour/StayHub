using FluentAssertions;
using StayHub.Domain.Favorites;

namespace StayHub.Domain.UnitTests.Favorites;

public class FavoriteApartmentTests
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var apartmentId = Guid.CreateVersion7();
        var utcNow = DateTime.UtcNow;

        // Act
        var favorite = FavoriteApartment.Create(userId, apartmentId, utcNow);

        // Assert
        favorite.UserId.Should().Be(userId);
        favorite.ApartmentId.Should().Be(apartmentId);
        favorite.CreatedOnUtc.Should().Be(utcNow);
    }
}