using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.Commands.Orders;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CleanArchitecture.IntegrationTests.Controllers;

public class OrdersControllerTests : IClassFixture<WebApplicationFactoryBase<Program>>
{
    private readonly WebApplicationFactoryBase<Program> _factory;
    private readonly HttpClient _client;

    public OrdersControllerTests(WebApplicationFactoryBase<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateOrder_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Empty customer ID
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.Empty,
            Items = new List<CreateOrderItemDto>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrderById_WithExistingId_ShouldReturnOk()
    {
        // Arrange - First create an order
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var createCommand = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 100.00m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Get the created order
        var response = await _client.GetAsync($"/api/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(orderId);
        order.CustomerId.Should().Be(customerId);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(100.00m);
    }

    [Fact]
    public async Task GetOrderById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/orders/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOrder_WithValidData_ShouldReturnNoContent()
    {
        // Arrange - Create an order first
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var createCommand = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 50.00m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Update the order
        var updateCommand = new UpdateOrderCommand
        {
            OrderId = orderId,
            Status = "Confirmed"
        };

        var response = await _client.PutAsJsonAsync($"/api/orders/{orderId}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteOrder_WithExistingId_ShouldReturnNoContent()
    {
        // Arrange - Create an order first
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var createCommand = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 50.00m
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Delete the order
        var response = await _client.DeleteAsync($"/api/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMultipleOrders_ShouldAllBeRetrievable()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderIds = new List<Guid>();

        // Act - Create 3 orders
        for (int i = 0; i < 3; i++)
        {
            var command = new CreateOrderCommand
            {
                CustomerId = customerId,
                Items = new List<CreateOrderItemDto>
                {
                    new()
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = i + 1,
                        UnitPrice = 50.00m
                    }
                }
            };

            var response = await _client.PostAsJsonAsync("/api/orders", command);
            var orderId = await response.Content.ReadFromJsonAsync<Guid>();
            orderIds.Add(orderId);
        }

        // Assert - Verify all orders can be retrieved
        var getResponse = await _client.GetAsync("/api/orders");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await getResponse.Content.ReadFromJsonAsync<List<OrderDto>>();
        orders.Should().NotBeNull();
        orders!.Count.Should().BeGreaterOrEqualTo(3);

        foreach (var orderId in orderIds)
        {
            orders.Should().Contain(o => o.Id == orderId);
        }
    }
}
