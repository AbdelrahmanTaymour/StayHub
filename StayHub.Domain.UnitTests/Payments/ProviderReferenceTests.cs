using FluentAssertions;
using StayHub.Domain.Payments;

namespace StayHub.Domain.UnitTests.Payments;

public class ProviderReferenceTests
{
    [Fact]
    public void ImplicitConversion_ToString_Should_ReturnUnderlyingValue()
    {
        // Arrange
        var reference = new ProviderReference("pi_test_123");

        // Act
        string value = reference;

        // Assert
        value.Should().Be("pi_test_123");
    }

    [Fact]
    public void ImplicitConversion_FromString_Should_WrapValue()
    {
        // Arrange & Act 
        ProviderReference reference = "pi_test_123";

        // Assert
        reference.Value.Should().Be("pi_test_123");
    }

    [Fact]
    public void Equality_Should_HoldForSameValue()
    {
        // Arrange — record value equality matters here for matching a
        // webhook's payment intent id against a stored payment's reference.
        var first = new ProviderReference("pi_test_123");
        var second = new ProviderReference("pi_test_123");

        // Assert
        first.Should().Be(second);
    }
}