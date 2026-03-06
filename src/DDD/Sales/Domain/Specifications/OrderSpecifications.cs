using System.Linq.Expressions;
using DDD.Sales.Domain.Aggregates.Order;
using DDD.Sales.Domain.ValueObjects;

namespace DDD.Sales.Domain.Specifications;

/// <summary>
/// Specifications for Order queries
/// Encapsulates business logic for filtering orders
/// </summary>
public class OrderByCustomerSpecification : Specification<Order>
{
    private readonly CustomerId _customerId;

    public OrderByCustomerSpecification(CustomerId customerId)
    {
        _customerId = customerId;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.CustomerId == _customerId;
    }
}

public class OrderByStatusSpecification : Specification<Order>
{
    private readonly OrderStatus _status;

    public OrderByStatusSpecification(OrderStatus status)
    {
        _status = status;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status == _status;
    }
}

public class OrderAboveAmountSpecification : Specification<Order>
{
    private readonly Money _minimumAmount;

    public OrderAboveAmountSpecification(Money minimumAmount)
    {
        _minimumAmount = minimumAmount;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Total.Amount >= _minimumAmount.Amount &&
                       order.Total.Currency == _minimumAmount.Currency;
    }
}

public class OrderCreatedInPeriodSpecification : Specification<Order>
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public OrderCreatedInPeriodSpecification(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.CreatedAt >= _startDate && order.CreatedAt <= _endDate;
    }
}

// Example of combining specifications
public static class CommonOrderSpecifications
{
    public static Specification<Order> HighValueOrders()
    {
        var minimumAmount = Money.Create(1000, Currency.USD);
        return new OrderAboveAmountSpecification(minimumAmount);
    }

    public static Specification<Order> PendingOrders()
    {
        return new OrderByStatusSpecification(OrderStatus.Draft)
            .Or(new OrderByStatusSpecification(OrderStatus.Confirmed));
    }

    public static Specification<Order> CustomerHighValueOrders(CustomerId customerId)
    {
        return new OrderByCustomerSpecification(customerId)
            .And(HighValueOrders());
    }

    public static Specification<Order> RecentOrders(int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var endDate = DateTime.UtcNow;
        return new OrderCreatedInPeriodSpecification(startDate, endDate);
    }
}
