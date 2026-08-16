using FluentAssertions;
using StayHub.Domain.Shared;

namespace StayHub.Domain.UnitTests.Shared;

public class CurrencyTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("EGP")]
    public void FromCode_Should_ReturnMatchingCurrency_WhenCodeIsKnown(string code)
    {
        // Act
        var currency = Currency.FromCode(code);

        // Assert
        currency.Code.Should().Be(code);
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("")]
    [InlineData("usd")]
    public void FromCode_Should_Throw_WhenCodeIsUnknown(string code)
    {
        // Act
        var act = () => Currency.FromCode(code);

        // Assert
        act.Should().Throw<ApplicationException>();
    }

    [Fact]
    public void All_Should_ContainExactlyTheSupportedCurrencies()
    {
        // Assert
        Currency.All.Should().BeEquivalentTo([Currency.Usd, Currency.Eur, Currency.Egp]);
    }

    [Fact]
    public void All_Should_NotContainNone()
    {
        Currency.All.Should().NotContain(Currency.None);
    }
}