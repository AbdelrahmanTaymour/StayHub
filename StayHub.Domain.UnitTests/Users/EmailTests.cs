using FluentAssertions;
using StayHub.Domain.Users;

namespace StayHub.Domain.UnitTests.Users;

public class EmailTests
{
    [Theory]
    [InlineData("test@test.com")]
    [InlineData("first.last@sub.domain.co")]
    public void Create_Should_ReturnSuccess_WhenValueIsValid(string value)
    {
        // Act
        var result = Email.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("@test.com")]
    [InlineData("test@")]
    [InlineData("test@@test.com")]
    [InlineData("   @   ")]
    public void Create_Should_ReturnFailure_WhenValueIsInvalid(string value)
    {
        // Act
        var result = Email.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Email.Invalid);
    }

    [Theory]
    [InlineData("@test.com")]
    [InlineData("test@")]
    [InlineData("test@@test.com")]
    [InlineData("   @   ")]
    public void Create_Should_ReturnFailure_WhenValueHasAtSymbolButIsStillMalformed(string value)
    {
        // Act
        var result = Email.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Email.Invalid);
    }

    [Fact]
    public void ImplicitStringConversion_Should_ReturnUnderlyingValue()
    {
        // Arrange
        var email = Email.Create("test@test.com").Value;

        // Act
        string value = email;

        // Assert
        value.Should().Be("test@test.com");
    }

    [Fact]
    public void Create_Should_ReturnEqualInstances_ForSameValue()
    {
        // Arrange 
        var first = Email.Create("test@test.com").Value;
        var second = Email.Create("test@test.com").Value;

        // Assert
        first.Should().Be(second);
    }
}