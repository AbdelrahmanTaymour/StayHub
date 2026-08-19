using FluentAssertions;
using StayHub.Domain.Bookings;

namespace StayHub.Domain.UnitTests.Bookings;

public class DateRangeTests
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 10);

        // Act
        var range = DateRange.Create(start, end);

        // Assert
        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void Create_Should_Throw_WhenStartIsAfterEnd()
    {
        // Act
        var act = () => DateRange.Create(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 1));

        // Assert
        act.Should().Throw<ApplicationException>();
    }

    [Fact]
    public void Create_Should_Succeed_WhenStartEqualsEnd()
    {
        // Arrange
        var sameDay = new DateOnly(2026, 1, 1);

        // Act
        var range = DateRange.Create(sameDay, sameDay);

        // Assert
        range.Start.Should().Be(sameDay);
        range.End.Should().Be(sameDay);
    }

    [Fact]
    public void LengthInDays_Should_ReturnZero_WhenStartEqualsEnd()
    {
        // Arrange
        var sameDay = new DateOnly(2026, 1, 1);
        var range = DateRange.Create(sameDay, sameDay);

        // Act & Assert
        range.LengthInDays.Should().Be(0);
    }

    [Theory]
    [InlineData(1, 1, 1, 10, 9)] // Jan 1 - Jan 10 = 9 nights
    [InlineData(1, 1, 1, 2, 1)] // Jan 1 - Jan 2 = 1 night
    [InlineData(1, 31, 2, 1, 1)] // crosses a month boundary = 1 night
    public void LengthInDays_Should_ReturnCorrectDayCount(
        int startMonth, int startDay, int endMonth, int endDay, int expectedLength)
    {
        // Arrange
        var range = DateRange.Create(new DateOnly(2026, startMonth, startDay), new DateOnly(2026, endMonth, endDay));

        // Act & Assert
        range.LengthInDays.Should().Be(expectedLength);
    }
}