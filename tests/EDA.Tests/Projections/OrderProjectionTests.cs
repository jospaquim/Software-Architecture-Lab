using EDA.Application.Projections;
using EDA.Domain.Events;
using EDA.Domain.ReadModel;
using FluentAssertions;
using Xunit;

namespace EDA.Tests.Projections;

public class OrderProjectionTests
{
    private readonly OrderProjection _projection;

    public OrderProjectionTests()
    {
        _projection = new OrderProjection();
    }

    [Fact]
    public void Apply_OrderCreatedEvent_ShouldCreateReadModel()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var @event = new OrderCreatedEvent(orderId, customerId)
        {
            Version = 1,
            Timestamp = DateTime.UtcNow
        };

        var readModel = new OrderReadModel();

        // Act
        _projection.Apply(readModel, @event);

        // Assert
        readModel.OrderId.Should().Be(orderId);
        readModel.CustomerId.Should().Be(customerId);
        readModel.Status.Should().Be("Created");
        readModel.Items.Should().BeEmpty();
        readModel.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void Apply_ItemAddedEvent_ShouldAddItemAndUpdateTotal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var readModel = new OrderReadModel
        {
            OrderId = orderId,
            Status = "Created",
            Items = new List<OrderItemReadModel>(),
            TotalAmount = 0
        };

        var @event = new ItemAddedEvent(orderId, productId, "Product 1", 2, 50.00m)
        {
            Version = 2,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _projection.Apply(readModel, @event);

        // Assert
        readModel.Items.Should().HaveCount(1);
        readModel.Items[0].ProductId.Should().Be(productId);
        readModel.Items[0].ProductName.Should().Be("Product 1");
        readModel.Items[0].Quantity.Should().Be(2);
        readModel.Items[0].UnitPrice.Should().Be(50.00m);
        readModel.TotalAmount.Should().Be(100.00m);
    }

    [Fact]
    public void Apply_MultipleItemAddedEvents_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var readModel = new OrderReadModel
        {
            OrderId = orderId,
            Status = "Created",
            Items = new List<OrderItemReadModel>(),
            TotalAmount = 0
        };

        var event1 = new ItemAddedEvent(orderId, Guid.NewGuid(), "Product 1", 2, 50.00m)
        {
            Version = 2,
            Timestamp = DateTime.UtcNow
        };

        var event2 = new ItemAddedEvent(orderId, Guid.NewGuid(), "Product 2", 3, 30.00m)
        {
            Version = 3,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _projection.Apply(readModel, event1);
        _projection.Apply(readModel, event2);

        // Assert
        readModel.Items.Should().HaveCount(2);
        readModel.TotalAmount.Should().Be(190.00m); // (2 * 50) + (3 * 30) = 190
    }

    [Fact]
    public void Apply_OrderConfirmedEvent_ShouldUpdateStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var readModel = new OrderReadModel
        {
            OrderId = orderId,
            Status = "Created",
            Items = new List<OrderItemReadModel>(),
            TotalAmount = 100.00m
        };

        var @event = new OrderConfirmedEvent(orderId)
        {
            Version = 3,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _projection.Apply(readModel, @event);

        // Assert
        readModel.Status.Should().Be("Confirmed");
        readModel.TotalAmount.Should().Be(100.00m); // Should not change
    }

    [Fact]
    public void Apply_OrderShippedEvent_ShouldUpdateStatusAndShippingDate()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var readModel = new OrderReadModel
        {
            OrderId = orderId,
            Status = "Confirmed",
            Items = new List<OrderItemReadModel>(),
            TotalAmount = 100.00m
        };

        var shippedDate = DateTime.UtcNow;
        var @event = new OrderShippedEvent(orderId, shippedDate)
        {
            Version = 4,
            Timestamp = shippedDate
        };

        // Act
        _projection.Apply(readModel, @event);

        // Assert
        readModel.Status.Should().Be("Shipped");
        readModel.ShippedAt.Should().Be(shippedDate);
    }

    [Fact]
    public void Apply_OrderCancelledEvent_ShouldUpdateStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var readModel = new OrderReadModel
        {
            OrderId = orderId,
            Status = "Created",
            Items = new List<OrderItemReadModel>(),
            TotalAmount = 100.00m
        };

        var @event = new OrderCancelledEvent(orderId, "Customer request")
        {
            Version = 5,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _projection.Apply(readModel, @event);

        // Assert
        readModel.Status.Should().Be("Cancelled");
        readModel.CancellationReason.Should().Be("Customer request");
    }

    [Fact]
    public void Apply_CompleteEventSequence_ShouldBuildCorrectReadModel()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var readModel = new OrderReadModel();

        var events = new List<IEvent>
        {
            new OrderCreatedEvent(orderId, customerId) { Version = 1, Timestamp = DateTime.UtcNow },
            new ItemAddedEvent(orderId, Guid.NewGuid(), "Product 1", 2, 50.00m) { Version = 2, Timestamp = DateTime.UtcNow },
            new ItemAddedEvent(orderId, Guid.NewGuid(), "Product 2", 1, 100.00m) { Version = 3, Timestamp = DateTime.UtcNow },
            new OrderConfirmedEvent(orderId) { Version = 4, Timestamp = DateTime.UtcNow }
        };

        // Act
        foreach (var @event in events)
        {
            _projection.Apply(readModel, @event);
        }

        // Assert
        readModel.OrderId.Should().Be(orderId);
        readModel.CustomerId.Should().Be(customerId);
        readModel.Status.Should().Be("Confirmed");
        readModel.Items.Should().HaveCount(2);
        readModel.TotalAmount.Should().Be(200.00m); // (2 * 50) + (1 * 100) = 200
    }

    [Fact]
    public void Apply_ItemRemovedEvent_ShouldRemoveItemAndUpdateTotal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var readModel = new OrderReadModel
        {
            OrderId = orderId,
            Status = "Created",
            Items = new List<OrderItemReadModel>
            {
                new() { ProductId = productId, ProductName = "Product 1", Quantity = 2, UnitPrice = 50.00m }
            },
            TotalAmount = 100.00m
        };

        var @event = new ItemRemovedEvent(orderId, productId)
        {
            Version = 3,
            Timestamp = DateTime.UtcNow
        };

        // Act
        _projection.Apply(readModel, @event);

        // Assert
        readModel.Items.Should().BeEmpty();
        readModel.TotalAmount.Should().Be(0);
    }
}
