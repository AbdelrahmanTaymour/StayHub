using FluentAssertions;
using StayHub.Domain.Shared;

namespace StayHub.Domain.UnitTests.Shared;

public class MoneyTests
{
    [Fact]
    public void Addition_Should_SumAmounts_WhenCurrenciesMatch()
    {
        // Arrange
        var first = new Money(10.50m, Currency.Usd);
        var second = new Money(5.25m, Currency.Usd);
        const decimal expectedAmount = 15.75m;

        // Act
        var result = first + second;

        // Assert
        result.Amount.Should().Be(expectedAmount);
        result.Currency.Should().Be(Currency.Usd);
    }

    [Fact]
    public void Addition_Should_ThrowException_WhenCurrenciesDiffer()
    {
        // Arrange
        var first = new Money(10.50m, Currency.Eur);
        var second = new Money(5.25m, Currency.Egp);

        // Act
        var act = () => first + second;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Zero_ParameterlessOverload_Should_ReturnZeroAmountWithNoneCurrency()
    {
        // Act
        var money = Money.Zero();

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be(Currency.None);
    }

    [Fact]
    public void Zero_WithCurrency_Should_ReturnZeroAmountInThatCurrency()
    {
        // Act
        var money = Money.Zero(Currency.Usd);

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be(Currency.Usd);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0.01, false)]
    [InlineData(-0.01, false)]
    public void IsZero_Should_ReturnExpectedResult(decimal amount, bool expectedResult)
    {
        // Arrange
        var money = new Money(amount, Currency.Usd);

        // Act
        var result = money.IsZero();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void IsZero_Should_ReturnTrue_RegardlessOfCurrency()
    {
        // Arrange
        var usdZero = new Money(0, Currency.Usd);
        var eurZero = new Money(0, Currency.Eur);

        // Assert
        usdZero.IsZero().Should().BeTrue();
        eurZero.IsZero().Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_HoldForSameAmountAndCurrency()
    {
        // Arrange

        // REMARK: Money is a record; this pins the value-equality
        var first = new Money(99.99m, Currency.Usd);
        var second = new Money(99.99m, Currency.Usd);

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void Equality_Should_NotHold_ForSameAmountButDifferentCurrency()
    {
        // Arrange
        var usd = new Money(10.0m, Currency.Usd);
        var eur = new Money(10.0m, Currency.Eur);

        // Assert
        usd.Should().NotBe(eur);
    }
}