using DDD.Sales.Domain.ValueObjects;

namespace DDD.Sales.Domain.Aggregates.Order;

// Order Domain Events - Things that have happened in the order lifecycle

public sealed record OrderCreatedEvent(
    OrderId OrderId,
    CustomerId CustomerId) : DomainEvent;

public sealed record OrderItemAddedEvent(
    OrderId OrderId,
    ProductId ProductId,
    int Quantity) : DomainEvent;

public sealed record OrderItemRemovedEvent(
    OrderId OrderId,
    OrderItemId OrderItemId) : DomainEvent;

public sealed record DiscountAppliedEvent(
    OrderId OrderId,
    DiscountId DiscountId,
    Money DiscountAmount) : DomainEvent;

public sealed record OrderConfirmedEvent(
    OrderId OrderId,
    Money Total) : DomainEvent;

public sealed record OrderShippedEvent(
    OrderId OrderId,
    DateTime ShippedAt) : DomainEvent;

public sealed record OrderDeliveredEvent(
    OrderId OrderId,
    DateTime DeliveredAt) : DomainEvent;

public sealed record OrderCancelledEvent(
    OrderId OrderId,
    string Reason) : DomainEvent;
