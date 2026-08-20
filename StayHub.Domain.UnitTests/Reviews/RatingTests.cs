using FluentAssertions;
using StayHub.Domain.Reviews;

namespace StayHub.Domain.UnitTests.Reviews;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Create_Should_ReturnSuccess_WhenValueIsWithinRange(int value)
    {
        // Act
        var result = Rating.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_Should_ReturnFailure_WhenValueIsOutOfRange(int value)
    {
        // Act
        var result = Rating.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Rating.Invalid);
    }
}