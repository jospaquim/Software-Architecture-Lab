using System.Net;
using System.Net.Http.Json;
using EDA.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace EDA.IntegrationTests.Controllers;

public class EventSourcingIntegrationTests : IClassFixture<EdaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EventSourcingIntegrationTests(EdaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_ShouldStoreEventAndBuildReadModel()
    {
        // Arrange
        var command = new
        {
            CustomerId = Guid.NewGuid()
        };

        // Act - Create order (stores OrderCreatedEvent)
        var createResponse = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        orderId.Should().NotBeEmpty();

        // Verify read model was built from event
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddItemToOrder_ShouldAppendEventAndUpdateReadModel()
    {
        // Arrange - Create an order first
        var createCommand = new
        {
            CustomerId = Guid.NewGuid()
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Add item (stores ItemAddedEvent)
        var addItemCommand = new
        {
            OrderId = orderId,
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 2,
            UnitPrice = 50.00m
        };

        var addItemResponse = await _client.PostAsJsonAsync($"/api/orders/{orderId}/items", addItemCommand);

        // Assert
        addItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify read model was updated
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderReadModel>();

        order.Should().NotBeNull();
        order!.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(100.00m); // 2 * 50
    }

    [Fact]
    public async Task EventReplay_ShouldReconstructState()
    {
        // Arrange - Create order and add multiple items
        var customerId = Guid.NewGuid();
        var createCommand = new { CustomerId = customerId };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Add multiple items (multiple events)
        await _client.PostAsJsonAsync($"/api/orders/{orderId}/items", new
        {
            OrderId = orderId,
            ProductId = Guid.NewGuid(),
            ProductName = "Product 1",
            Quantity = 2,
            UnitPrice = 50.00m
        });

        await _client.PostAsJsonAsync($"/api/orders/{orderId}/items", new
        {
            OrderId = orderId,
            ProductId = Guid.NewGuid(),
            ProductName = "Product 2",
            Quantity = 1,
            UnitPrice = 100.00m
        });

        // Act - Get the order (read model built from event replay)
        var response = await _client.GetAsync($"/api/orders/{orderId}");

        // Assert - Verify state was reconstructed correctly
        var order = await response.Content.ReadFromJsonAsync<OrderReadModel>();
        order.Should().NotBeNull();
        order!.OrderId.Should().Be(orderId);
        order.Items.Should().HaveCount(2);
        order.TotalAmount.Should().Be(200.00m); // (2 * 50) + (1 * 100)
    }

    [Fact]
    public async Task ConfirmOrder_ShouldStoreEventAndChangeStatus()
    {
        // Arrange - Create order
        var createCommand = new { CustomerId = Guid.NewGuid() };
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Confirm order (stores OrderConfirmedEvent)
        var confirmResponse = await _client.PostAsync($"/api/orders/{orderId}/confirm", null);

        // Assert
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status changed in read model
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderReadModel>();

        order.Should().NotBeNull();
        order!.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task CancelOrder_ShouldStoreEventWithReason()
    {
        // Arrange - Create order
        var createCommand = new { CustomerId = Guid.NewGuid() };
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Cancel order (stores OrderCancelledEvent)
        var cancelCommand = new
        {
            Reason = "Customer request"
        };

        var cancelResponse = await _client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", cancelCommand);

        // Assert
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status and reason in read model
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderReadModel>();

        order.Should().NotBeNull();
        order!.Status.Should().Be("Cancelled");
        order.CancellationReason.Should().Be("Customer request");
    }

    [Fact]
    public async Task GetAllOrders_ShouldReturnAllReadModels()
    {
        // Arrange - Create multiple orders
        var order1Response = await _client.PostAsJsonAsync("/api/orders", new { CustomerId = Guid.NewGuid() });
        var order2Response = await _client.PostAsJsonAsync("/api/orders", new { CustomerId = Guid.NewGuid() });
        var order3Response = await _client.PostAsJsonAsync("/api/orders", new { CustomerId = Guid.NewGuid() });

        var orderId1 = await order1Response.Content.ReadFromJsonAsync<Guid>();
        var orderId2 = await order2Response.Content.ReadFromJsonAsync<Guid>();
        var orderId3 = await order3Response.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderReadModel>>();
        orders.Should().NotBeNull();
        orders!.Should().HaveCountGreaterOrEqualTo(3);
        orders.Should().Contain(o => o.OrderId == orderId1);
        orders.Should().Contain(o => o.OrderId == orderId2);
        orders.Should().Contain(o => o.OrderId == orderId3);
    }

    private class OrderReadModel
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemReadModel> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class OrderItemReadModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
