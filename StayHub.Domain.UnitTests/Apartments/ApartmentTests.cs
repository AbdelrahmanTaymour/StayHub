using FluentAssertions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.Shared;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Apartments;

public class ApartmentTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Act
        var apartment = ApartmentData.Create();

        // Assert
        apartment.Id.Should().NotBeEmpty();
        apartment.Name.Should().Be(ApartmentData.Name);
        apartment.Description.Should().Be(ApartmentData.Description);
        apartment.Address.Should().Be(ApartmentData.Address);
        apartment.Price.Should().Be(ApartmentData.Price);
        apartment.CleaningFee.Should().Be(ApartmentData.CleaningFee);
        apartment.IsActive.Should().BeTrue();
        apartment.LastBookedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Create_Should_RaiseApartmentCreatedDomainEvent()
    {
        // Act
        var apartment = ApartmentData.Create();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentCreatedDomainEvent>(apartment);
        domainEvent.ApartmentId.Should().Be(apartment.Id);
    }

    [Fact]
    public void UpdateDetails_Should_SetPropertyValues()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var newName = new Name("Updated apartment");
        var newDescription = new Description("Updated description");
        var newPrice = new Money(150m, Currency.Usd);
        var newCleaningFee = new Money(20m, Currency.Usd);

        // Act
        apartment.UpdateDetails(newName, newDescription, newPrice, newCleaningFee);

        // Assert
        apartment.Name.Should().Be(newName);
        apartment.Description.Should().Be(newDescription);
        apartment.Price.Should().Be(newPrice);
        apartment.CleaningFee.Should().Be(newCleaningFee);
    }

    [Fact]
    public void UpdateDetails_Should_RaiseApartmentUpdatedDomainEvent()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        // Act
        apartment.UpdateDetails(
            new Name("Updated"),
            new Description("Updated"),
            ApartmentData.Price,
            Money.Zero(Currency.Usd));

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentUpdatedDomainEvent>(apartment);
        domainEvent.ApartmentId.Should().Be(apartment.Id);
    }

    [Fact]
    public void AddAmenity_Should_AddAmenityAndReturnSuccess_WhenNotAlreadyPresent()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        // Act
        var result = apartment.AddAmenity(Amenity.WiFi);

        // Assert
        result.IsSuccess.Should().BeTrue();
        apartment.Amenities.Should().Contain(Amenity.WiFi);
    }

    [Fact]
    public void AddAmenity_Should_RaiseApartmentAmenitiesChangedDomainEvent_WhenAdded()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        // Act
        apartment.AddAmenity(Amenity.WiFi);

        // Assert
        AssertDomainEventWasPublished<ApartmentAmenitiesChangedDomainEvent>(apartment);
    }

    [Fact]
    public void AddAmenity_Should_ReturnFailure_WhenAmenityAlreadyAdded()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(Amenity.WiFi);

        // Act
        var result = apartment.AddAmenity(Amenity.WiFi);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.AmenityAlreadyAdded);
    }

    [Fact]
    public void AddAmenity_Should_NotAddDuplicate_WhenAmenityAlreadyAdded()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(Amenity.WiFi);

        // Act
        apartment.AddAmenity(Amenity.WiFi);

        // Assert
        apartment.Amenities.Should().ContainSingle(a => a == Amenity.WiFi);
    }

    [Fact]
    public void AddAmenity_Should_NotRaiseDomainEventAgain_WhenAmenityAlreadyAdded()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(Amenity.WiFi);

        // Act
        apartment.AddAmenity(Amenity.WiFi);

        // Assert — one successful add, one rejected duplicate: exactly one event.
        AssertDomainEventWasPublishedTimes<ApartmentAmenitiesChangedDomainEvent>(apartment, 1);
    }

    [Fact]
    public void RemoveAmenity_Should_RemoveAmenityAndReturnSuccess_WhenAmenityExists()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(Amenity.WiFi);

        // Act
        var result = apartment.RemoveAmenity(Amenity.WiFi);

        // Assert
        result.IsSuccess.Should().BeTrue();
        apartment.Amenities.Should().NotContain(Amenity.WiFi);
    }

    [Fact]
    public void RemoveAmenity_Should_ReturnFailure_WhenAmenityDoesNotExist()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        // Act
        var result = apartment.RemoveAmenity(Amenity.WiFi);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.AmenityNotFound);
    }

    [Fact]
    public void RemoveAmenity_Should_RaiseApartmentAmenitiesChangedDomainEvent_WhenRemoved()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.AddAmenity(Amenity.WiFi);
        apartment.ClearDomainEvents();

        // Act
        apartment.RemoveAmenity(Amenity.WiFi);

        // Assert
        AssertDomainEventWasPublished<ApartmentAmenitiesChangedDomainEvent>(apartment);
    }

    [Fact]
    public void RemoveAmenity_Should_NotRaiseDomainEvent_WhenAmenityDoesNotExist()
    {
        // Arrange
        var apartment = ApartmentData.Create();

        // Act
        apartment.RemoveAmenity(Amenity.WiFi);

        // Assert — a rejected removal is not a "change" worth notifying about.
        AssertDomainEventWasNotPublished<ApartmentAmenitiesChangedDomainEvent>(apartment);
    }

    [Fact]
    public void Activate_Should_SetIsActiveTrueAndReturnSuccess_WhenCurrentlyInactive()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.Deactivate();

        // Act
        var result = apartment.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        apartment.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_Should_RaiseApartmentActivatedDomainEvent_WhenCurrentlyInactive()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.Deactivate();
        apartment.ClearDomainEvents();

        // Act
        apartment.Activate();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentActivatedDomainEvent>(apartment);
        domainEvent.ApartmentId.Should().Be(apartment.Id);
    }

    [Fact]
    public void Activate_Should_ReturnFailure_WhenAlreadyActive()
    {
        // Arrange 
        var apartment = ApartmentData.Create();
        apartment.Activate();

        // Act
        var result = apartment.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.AlreadyActive);
    }

    [Fact]
    public void Activate_Should_NotRaiseDomainEvent_WhenAlreadyActive()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.Activate();
        apartment.ClearDomainEvents();

        // Act
        apartment.Activate();

        // Assert
        AssertDomainEventWasNotPublished<ApartmentActivatedDomainEvent>(apartment);
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveFalseAndReturnSuccess_WhenCurrentlyActive()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.Activate();

        // Act
        var result = apartment.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        apartment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Should_RaiseApartmentDeactivatedDomainEvent_WhenCurrentlyActive()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.Activate();

        // Act
        apartment.Deactivate();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentDeactivatedDomainEvent>(apartment);
        domainEvent.ApartmentId.Should().Be(apartment.Id);
    }

    [Fact]
    public void Deactivate_Should_ReturnFailure_WhenAlreadyInactive()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.Deactivate();

        // Act
        var result = apartment.Deactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.AlreadyInactive);
    }

    [Fact]
    public void Deactivate_Should_NotRaiseDomainEvent_WhenAlreadyInactive()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.Deactivate();
        apartment.ClearDomainEvents();

        // Act
        apartment.Deactivate();

        // Assert
        AssertDomainEventWasNotPublished<ApartmentDeactivatedDomainEvent>(apartment);
    }

    [Fact]
    public void UpdateLastBooked_Should_SetLastBookedOnUtc()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var utcNow = DateTime.UtcNow;

        // Act
        apartment.UpdateLastBooked(utcNow);

        // Assert
        apartment.LastBookedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void UpdateLastBooked_Should_OverwritePreviousValue_WhenCalledAgain()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        apartment.UpdateLastBooked(DateTime.UtcNow.AddDays(-5));
        var latestUtcNow = DateTime.UtcNow;

        // Act
        apartment.UpdateLastBooked(latestUtcNow);

        // Assert
        apartment.LastBookedOnUtc.Should().Be(latestUtcNow);
    }
}