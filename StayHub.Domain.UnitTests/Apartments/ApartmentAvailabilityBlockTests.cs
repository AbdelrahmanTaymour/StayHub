using FluentAssertions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Apartments;

public class ApartmentAvailabilityBlockTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var start = new DateOnly(2026, 3, 1);
        var end = new DateOnly(2026, 3, 10);
        var utcNow = DateTime.UtcNow;

        // Act
        var block = ApartmentAvailabilityBlock.Create(
            apartmentId,
            start,
            end,
            ApartmentUnavailabilityReason.UnderMaintenance,
            utcNow);

        // Assert
        block.ApartmentId.Should().Be(apartmentId);
        block.Start.Should().Be(start);
        block.End.Should().Be(end);
        block.Reason.Should().Be(ApartmentUnavailabilityReason.UnderMaintenance);
        block.CreatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void Create_Should_RaiseApartmentAvailabilityBlockCreatedDomainEvent()
    {
        // Act
        var block = ApartmentData.CreateAvailabilityBlock();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ApartmentAvailabilityBlockCreatedDomainEvent>(block);
        domainEvent.Id.Should().Be(block.Id);
        domainEvent.ApartmentId.Should().Be(block.ApartmentId);
        domainEvent.Start.Should().Be(block.Start);
        domainEvent.End.Should().Be(block.End);
    }

    [Fact]
    public void Create_Should_Throw_WhenStartIsAfterEnd()
    {
        // Act
        var act = () => ApartmentAvailabilityBlock.Create(
            Guid.CreateVersion7(),
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 1),
            ApartmentUnavailabilityReason.OwnerBlocked,
            DateTime.UtcNow);

        // Assert
        act.Should().Throw<ApplicationException>();
    }

    [Fact]
    public void Create_Should_Succeed_WhenStartEqualsEnd()
    {
        // Arrange — a single-day block is a valid boundary, not an error;
        // the guard is "start > end", not "start >= end".
        var singleDay = new DateOnly(2026, 3, 1);

        // Act
        var block = ApartmentData.CreateAvailabilityBlock(start: singleDay, end: singleDay);

        // Assert
        block.Start.Should().Be(singleDay);
        block.End.Should().Be(singleDay);
    }

    [Theory]
    [InlineData(2026, 3, 5, 2026, 3, 15)]
    [InlineData(2026, 2, 20, 2026, 3, 5)]
    [InlineData(2026, 2, 1, 2026, 4, 1)]
    [InlineData(2026, 3, 3, 2026, 3, 7)]
    public void Overlaps_Should_ReturnTrue_WhenRangesOverlap(
        int otherStartYear, int otherStartMonth, int otherStartDay,
        int otherEndYear, int otherEndMonth, int otherEndDay)
    {
        // Arrange — block spans March 1 to March 10
        var block = ApartmentData.CreateAvailabilityBlock(
            start: new DateOnly(2026, 3, 1),
            end: new DateOnly(2026, 3, 10));

        var otherStart = new DateOnly(otherStartYear, otherStartMonth, otherStartDay);
        var otherEnd = new DateOnly(otherEndYear, otherEndMonth, otherEndDay);

        // Act
        var result = block.Overlaps(otherStart, otherEnd);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 2, 1, 2026, 2, 20)]
    [InlineData(2026, 3, 20, 2026, 3, 25)]
    public void Overlaps_Should_ReturnFalse_WhenRangesDoNotOverlap(
        int otherStartYear, int otherStartMonth, int otherStartDay,
        int otherEndYear, int otherEndMonth, int otherEndDay)
    {
        // Arrange
        var block = ApartmentData.CreateAvailabilityBlock(
            start: new DateOnly(2026, 3, 1),
            end: new DateOnly(2026, 3, 10));

        var otherStart = new DateOnly(otherStartYear, otherStartMonth, otherStartDay);
        var otherEnd = new DateOnly(otherEndYear, otherEndMonth, otherEndDay);

        // Act
        var result = block.Overlaps(otherStart, otherEnd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_Should_ReturnTrue_WhenRangesAreIdentical()
    {
        // Arrange
        var block = ApartmentData.CreateAvailabilityBlock(
            start: new DateOnly(2026, 3, 1),
            end: new DateOnly(2026, 3, 10));

        // Act
        var result = block.Overlaps(
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 10));

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(2026, 3, 10, 2026, 3, 15)] // another range starts exactly on this block's End
    [InlineData(2026, 2, 20, 2026, 3, 1)] // another range ends exactly on this block's Start
    public void Overlaps_Should_ReturnTrue_WhenRangesTouchAtBoundary(
        int otherStartYear, int otherStartMonth, int otherStartDay,
        int otherEndYear, int otherEndMonth, int otherEndDay)
    {
        // Arrange
        var block = ApartmentData.CreateAvailabilityBlock(
            start: new DateOnly(2026, 3, 1),
            end: new DateOnly(2026, 3, 10));

        var otherStart = new DateOnly(otherStartYear, otherStartMonth, otherStartDay);
        var otherEnd = new DateOnly(otherEndYear, otherEndMonth, otherEndDay);

        // Act
        var result = block.Overlaps(otherStart, otherEnd);

        // Assert
        result.Should().BeTrue();
    }
}