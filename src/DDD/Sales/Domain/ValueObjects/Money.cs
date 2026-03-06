namespace DDD.Sales.Domain.ValueObjects;

/// <summary>
/// Value Object for Money - Immutable
/// Follows DDD pattern: no identity, defined by attributes
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency) => new(0, currency);

    // Business operations
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add money with different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract money with different currencies");

        var result = Amount - other.Amount;
        if (result < 0)
            throw new InvalidOperationException("Subtraction would result in negative amount");

        return new Money(result, Currency);
    }

    public Money Multiply(decimal multiplier)
    {
        if (multiplier < 0)
            throw new ArgumentException("Multiplier cannot be negative");

        return new Money(Amount * multiplier, Currency);
    }

    public Money ApplyDiscount(decimal percentage)
    {
        if (percentage < 0 || percentage > 1)
            throw new ArgumentException("Percentage must be between 0 and 1");

        return new Money(Amount * (1 - percentage), Currency);
    }

    // Equality
    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money? left, Money? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");

        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");

        return left.Amount < right.Amount;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}

public enum Currency
{
    USD,
    EUR,
    GBP,
    JPY,
    MXN
}
