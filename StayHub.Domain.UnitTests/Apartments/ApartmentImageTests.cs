using FluentAssertions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Apartments;

public class ApartmentImageTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        DateTime utcNow = DateTime.UtcNow;
        var apartmentId = Guid.CreateVersion7();
        var url = new ImageUrl("https://cdn.stayhub.dev/images/test.png");

        // Act
        var image = ApartmentImage.Create(apartmentId, url, displayOrder: 2, utcNow);

        // Assert
        image.Id.Should().NotBeEmpty();
        image.ApartmentId.Should().Be(apartmentId);
        image.Url.Should().Be(url);
        image.DisplayOrder.Should().Be(2);
        image.IsPrimary.Should().BeFalse();
        image.CreatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Create_Should_SetIsPrimaryTrue_WhenSpecified()
    {
        // Act
        var image = ApartmentData.CreateImage(isPrimary: true);

        // Assert
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_RaiseApartmentImageAddedDomainEvent()
    {
        // Act
        var image = ApartmentData.CreateImage();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentImageAddedDomainEvent>(image);
        domainEvent.ImageId.Should().Be(image.Id);
        domainEvent.ApartmentId.Should().Be(image.ApartmentId);
    }

    [Fact]
    public void SetAsPrimary_Should_SetIsPrimaryTrue()
    {
        // Arrange
        var image = ApartmentData.CreateImage();

        // Act
        image.SetAsPrimary();

        // Assert
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SetAsPrimary_Should_RaiseApartmentImageUpdatedDomainEvent()
    {
        // Arrange
        var image = ApartmentData.CreateImage();
        image.ClearDomainEvents();

        // Act
        image.SetAsPrimary();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentImageUpdatedDomainEvent>(image);
        domainEvent.ImageId.Should().Be(image.Id);
        domainEvent.ApartmentId.Should().Be(image.ApartmentId);
    }

    [Fact]
    public void Reorder_Should_SetDisplayOrder()
    {
        // Arrange
        var image = ApartmentData.CreateImage(displayOrder: 0);

        // Act
        image.Reorder(5);

        // Assert
        image.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public void MarkForRemoval_Should_RaiseApartmentImageRemovedDomainEvent_WithCorrectPayload()
    {
        // Arrange
        var image = ApartmentData.CreateImage();
        image.ClearDomainEvents();

        // Act
        image.MarkForRemoval();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentImageRemovedDomainEvent>(image);
        domainEvent.ImageId.Should().Be(image.Id);
        domainEvent.ApartmentId.Should().Be(image.ApartmentId);
        domainEvent.Url.Should().Be(image.Url);
    }
}