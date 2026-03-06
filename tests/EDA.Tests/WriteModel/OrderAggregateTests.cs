using EDA.WriteModel.Domain;
using EDA.WriteModel.Domain.Events;
using FluentAssertions;
using Xunit;

namespace EDA.Tests.WriteModel;

/// <summary>
/// Tests para OrderAggregate con Event Sourcing
/// Verifican que los eventos se generen correctamente y que el estado se reconstruya
/// </summary>
public class OrderAggregateTests
{
    [Fact]
    public void CreateNew_ShouldGenerateOrderCreatedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var order = OrderAggregate.CreateNew(customerId);

        // Assert
        order.Id.Should().NotBe(Guid.Empty);
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be("Draft");
        order.Items.Should().BeEmpty();

        // Verificar evento generado
        order.UncommittedEvents.Should().HaveCount(1);
        var @event = order.UncommittedEvents.First() as OrderCreatedEvent;
        @event.Should().NotBeNull();
        @event!.CustomerId.Should().Be(customerId);
        @event.Version.Should().Be(1);
    }

    [Fact]
    public void AddItem_ShouldGenerateItemAddedEvent()
    {
        // Arrange
        var order = OrderAggregate.CreateNew(Guid.NewGuid());
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var quantity = 2;
        var unitPrice = 50.00m;

        // Act
        order.AddItem(productId, productName, quantity, unitPrice);

        // Assert
        order.Items.Should().HaveCount(1);
        order.Items[0].ProductName.Should().Be(productName);
        order.Items[0].Quantity.Should().Be(quantity);
        order.Items[0].UnitPrice.Should().Be(unitPrice);

        // Verificar eventos (2: OrderCreated + ItemAdded)
        order.UncommittedEvents.Should().HaveCount(2);
        var itemAddedEvent = order.UncommittedEvents.Last() as ItemAddedEvent;
        itemAddedEvent.Should().NotBeNull();
        itemAddedEvent!.ProductId.Should().Be(productId);
        itemAddedEvent.Version.Should().Be(2);
    }

    [Fact]
    public void Confirm_ShouldGenerateOrderConfirmedEvent()
    {
        // Arrange
        var order = OrderAggregate.CreateNew(Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be("Confirmed");

        // Verificar evento
        var confirmedEvent = order.UncommittedEvents.Last() as OrderConfirmedEvent;
        confirmedEvent.Should().NotBeNull();
        confirmedEvent!.Version.Should().Be(3); // Create + AddItem + Confirm
    }

    [Fact]
    public void Ship_ShouldGenerateOrderShippedEvent()
    {
        // Arrange
        var order = OrderAggregate.CreateNew(Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);
        order.Confirm();

        // Act
        order.Ship();

        // Assert
        order.Status.Should().Be("Shipped");

        var shippedEvent = order.UncommittedEvents.Last() as OrderShippedEvent;
        shippedEvent.Should().NotBeNull();
        shippedEvent!.Version.Should().Be(4);
    }

    [Fact]
    public void LoadFromHistory_ShouldReconstructState()
    {
        // Arrange - Crear eventos históricos
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var events = new List<IEvent>
        {
            new OrderCreatedEvent(orderId, customerId) { Version = 1, OccurredAt = DateTime.UtcNow },
            new ItemAddedEvent(orderId, productId, "Product", 2, 50.00m) { Version = 2, OccurredAt = DateTime.UtcNow },
            new OrderConfirmedEvent(orderId) { Version = 3, OccurredAt = DateTime.UtcNow }
        };

        // Act - Reconstruir desde historial
        var order = OrderAggregate.LoadFromHistory(orderId, events);

        // Assert - Verificar estado reconstruido
        order.Id.Should().Be(orderId);
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be("Confirmed");
        order.Items.Should().HaveCount(1);
        order.Items[0].ProductId.Should().Be(productId);
        order.Items[0].Quantity.Should().Be(2);
        order.Version.Should().Be(3);
        order.UncommittedEvents.Should().BeEmpty(); // No hay eventos nuevos
    }

    [Fact]
    public void LoadFromHistory_WithMultipleItems_ShouldReconstructAllItems()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var events = new List<IEvent>
        {
            new OrderCreatedEvent(orderId, customerId) { Version = 1, OccurredAt = DateTime.UtcNow },
            new ItemAddedEvent(orderId, Guid.NewGuid(), "Product 1", 1, 10.00m) { Version = 2, OccurredAt = DateTime.UtcNow },
            new ItemAddedEvent(orderId, Guid.NewGuid(), "Product 2", 2, 20.00m) { Version = 3, OccurredAt = DateTime.UtcNow },
            new ItemAddedEvent(orderId, Guid.NewGuid(), "Product 3", 3, 30.00m) { Version = 4, OccurredAt = DateTime.UtcNow }
        };

        // Act
        var order = OrderAggregate.LoadFromHistory(orderId, events);

        // Assert
        order.Items.Should().HaveCount(3);
        order.Version.Should().Be(4);
    }

    [Fact]
    public void MarkChangesAsCommitted_ShouldClearUncommittedEvents()
    {
        // Arrange
        var order = OrderAggregate.CreateNew(Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);

        // Act
        order.MarkChangesAsCommitted();

        // Assert
        order.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void EventVersioning_ShouldIncrementCorrectly()
    {
        // Arrange
        var order = OrderAggregate.CreateNew(Guid.NewGuid());

        // Act
        order.AddItem(Guid.NewGuid(), "Product 1", 1, 10.00m);
        order.AddItem(Guid.NewGuid(), "Product 2", 1, 20.00m);
        order.Confirm();

        // Assert
        var events = order.UncommittedEvents.ToList();
        events[0].Version.Should().Be(1); // OrderCreated
        events[1].Version.Should().Be(2); // First ItemAdded
        events[2].Version.Should().Be(3); // Second ItemAdded
        events[3].Version.Should().Be(4); // OrderConfirmed
    }
}
