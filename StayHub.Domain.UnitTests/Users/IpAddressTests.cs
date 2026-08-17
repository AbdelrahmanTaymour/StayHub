using FluentAssertions;
using StayHub.Domain.Users;

namespace StayHub.Domain.UnitTests.Users;

public class IpAddressTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("2001:db8::ff00:42:8329")]
    public void Create_Should_ReturnSuccess_WhenValueIsValid(string value)
    {
        // Act
        var result = IpAddress.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    [InlineData("256.1.1.1")]
    [InlineData("1.2.3")]
    public void Create_Should_ReturnFailure_WhenValueIsInvalid(string? value)
    {
        // Act
        var result = IpAddress.Create(value!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IpAddress.Invalid);
    }

    [Fact]
    public void Create_Should_ReturnEqualInstances_ForSameValue()
    {
        // Act
        var first = IpAddress.Create("127.0.0.1").Value;
        var second = IpAddress.Create("127.0.0.1").Value;

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void ImplicitStringConversion_Should_ReturnUnderlyingValue()
    {
        // Arrange
        var ipAddress = IpAddress.Create("127.0.0.1").Value;

        // Act
        string value = ipAddress;

        // Assert
        value.Should().Be("127.0.0.1");
    }
}