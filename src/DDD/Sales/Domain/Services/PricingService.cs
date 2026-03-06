using DDD.Sales.Domain.Aggregates.Order;
using DDD.Sales.Domain.ValueObjects;

namespace DDD.Sales.Domain.Services;

/// <summary>
/// Domain Service for pricing calculations
/// Used when logic involves multiple aggregates or doesn't naturally belong to one entity
/// </summary>
public interface IPricingService
{
    Money CalculateOrderTotal(Order order, CustomerType customerType, DiscountPolicy? discountPolicy);
    Money CalculateTax(Money amount, string country);
    bool IsEligibleForFreeShipping(Order order, CustomerType customerType);
}

public class PricingService : IPricingService
{
    public Money CalculateOrderTotal(Order order, CustomerType customerType, DiscountPolicy? discountPolicy)
    {
        var subtotal = order.Subtotal;

        // Apply customer type discount
        subtotal = customerType switch
        {
            CustomerType.Vip => subtotal.ApplyDiscount(0.10m), // 10% off for VIP
            CustomerType.Premium => subtotal.ApplyDiscount(0.05m), // 5% off for Premium
            _ => subtotal
        };

        // Apply discount policy if exists
        if (discountPolicy != null && discountPolicy.IsValid())
        {
            subtotal = discountPolicy.ApplyDiscount(subtotal);
        }

        // Add tax
        var tax = CalculateTax(subtotal, order.ShippingAddress.Country);

        return subtotal.Add(tax);
    }

    public Money CalculateTax(Money amount, string country)
    {
        // Tax rates by country (simplified - in real world, use tax service)
        var taxRate = country.ToUpperInvariant() switch
        {
            "US" or "USA" => 0.08m,  // 8% sales tax
            "UK" or "GB" => 0.20m,   // 20% VAT
            "EU" => 0.21m,           // 21% VAT (simplified)
            "MX" => 0.16m,           // 16% IVA
            _ => 0.00m               // No tax for unknown countries
        };

        return amount.Multiply(taxRate);
    }

    public bool IsEligibleForFreeShipping(Order order, CustomerType customerType)
    {
        // Business rules for free shipping
        if (customerType == CustomerType.Vip)
            return true;

        if (order.Subtotal.Amount >= 100) // Free shipping over $100
            return true;

        if (order.Items.Count >= 5) // Free shipping for 5+ items
            return true;

        return false;
    }
}

public enum CustomerType
{
    Regular,
    Premium,
    Vip
}

/// <summary>
/// Discount Policy Value Object
/// </summary>
public sealed class DiscountPolicy
{
    public string Code { get; }
    public decimal Percentage { get; }
    public DateTime ValidFrom { get; }
    public DateTime ValidTo { get; }
    public Money? MinimumAmount { get; }

    private DiscountPolicy(string code, decimal percentage, DateTime validFrom, DateTime validTo, Money? minimumAmount)
    {
        Code = code;
        Percentage = percentage;
        ValidFrom = validFrom;
        ValidTo = validTo;
        MinimumAmount = minimumAmount;
    }

    public static DiscountPolicy Create(string code, decimal percentage, DateTime validFrom, DateTime validTo, Money? minimumAmount = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Discount code is required", nameof(code));

        if (percentage <= 0 || percentage > 1)
            throw new ArgumentException("Percentage must be between 0 and 1", nameof(percentage));

        if (validFrom >= validTo)
            throw new ArgumentException("Valid from must be before valid to");

        return new DiscountPolicy(code, percentage, validFrom, validTo, minimumAmount);
    }

    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return now >= ValidFrom && now <= ValidTo;
    }

    public bool CanApplyTo(Money amount)
    {
        if (MinimumAmount == null)
            return true;

        return amount > MinimumAmount;
    }

    public Money ApplyDiscount(Money amount)
    {
        if (!IsValid())
            throw new InvalidOperationException("Discount policy is not valid");

        if (!CanApplyTo(amount))
            throw new InvalidOperationException($"Minimum amount of {MinimumAmount} required");

        return amount.ApplyDiscount(Percentage);
    }
}
