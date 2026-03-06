using DDD.Sales.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DDD.Sales.Domain.Tests.ValueObjects;

/// <summary>
/// Tests para Value Object Money
/// Los Value Objects deben ser inmutables y compararse por valor
/// </summary>
public class MoneyTests
{
    [Fact]
    public void Create_ShouldCreateMoneyWithCorrectValues()
    {
        // Arrange & Act
        var money = Money.Create(100.50m, Currency.USD);

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => Money.Create(-10m, Currency.USD);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount cannot be negative*");
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(100m, Currency.USD);

        // Act & Assert
        money1.Should().Be(money2); // Value equality
    }

    [Fact]
    public void Equals_WithDifferentAmounts_ShouldReturnFalse()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(200m, Currency.USD);

        // Act & Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void Equals_WithDifferentCurrencies_ShouldReturnFalse()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(100m, Currency.EUR);

        // Act & Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(50m, Currency.USD);

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(50m, Currency.EUR);

        // Act
        Action act = () => money1.Add(money2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add money with different currencies*");
    }

    [Fact]
    public void Subtract_WithSameCurrency_ShouldReturnDifference()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(30m, Currency.USD);

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Multiply_ShouldReturnMultipliedAmount()
    {
        // Arrange
        var money = Money.Create(50m, Currency.USD);

        // Act
        var result = money.Multiply(3);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Zero_ShouldCreateMoneyWithZeroAmount()
    {
        // Act
        var zero = Money.Zero(Currency.USD);

        // Assert
        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void IsZero_WithZeroAmount_ShouldReturnTrue()
    {
        // Arrange
        var money = Money.Zero(Currency.USD);

        // Act & Assert
        money.IsZero.Should().BeTrue();
    }

    [Fact]
    public void IsZero_WithNonZeroAmount_ShouldReturnFalse()
    {
        // Arrange
        var money = Money.Create(100m, Currency.USD);

        // Act & Assert
        money.IsZero.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ForEqualObjects_ShouldBeSame()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(100m, Currency.USD);

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }
}
