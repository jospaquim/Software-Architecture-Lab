namespace DDD.Sales.Domain.ValueObjects;

/// <summary>
/// Strongly Typed IDs - Prevents primitive obsession
/// Each entity has its own ID type
/// </summary>
public abstract record EntityId(Guid Value)
{
    public static T Create<T>() where T : EntityId
    {
        var guid = Guid.NewGuid();
        return (T)Activator.CreateInstance(typeof(T), guid)!;
    }

    public static T CreateFrom<T>(Guid value) where T : EntityId
    {
        return (T)Activator.CreateInstance(typeof(T), value)!;
    }

    public override string ToString() => Value.ToString();
}

// Specific ID types for each aggregate
public sealed record CustomerId(Guid Value) : EntityId(Value)
{
    public static CustomerId Create() => new(Guid.NewGuid());
    public static CustomerId CreateFrom(Guid value) => new(value);
}

public sealed record OrderId(Guid Value) : EntityId(Value)
{
    public static OrderId Create() => new(Guid.NewGuid());
    public static OrderId CreateFrom(Guid value) => new(value);
}

public sealed record OrderItemId(Guid Value) : EntityId(Value)
{
    public static OrderItemId Create() => new(Guid.NewGuid());
    public static OrderItemId CreateFrom(Guid value) => new(value);
}

public sealed record ProductId(Guid Value) : EntityId(Value)
{
    public static ProductId Create() => new(Guid.NewGuid());
    public static ProductId CreateFrom(Guid value) => new(value);
}

public sealed record DiscountId(Guid Value) : EntityId(Value)
{
    public static DiscountId Create() => new(Guid.NewGuid());
    public static DiscountId CreateFrom(Guid value) => new(value);
}
