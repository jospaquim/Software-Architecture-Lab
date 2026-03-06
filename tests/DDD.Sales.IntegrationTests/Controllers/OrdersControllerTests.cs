using System.Net;
using System.Net.Http.Json;
using DDD.Sales.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DDD.Sales.IntegrationTests.Controllers;

public class OrdersControllerTests : IClassFixture<DddWebApplicationFactory>
{
    private readonly DddWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrdersControllerTests(DddWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithValidValueObjects_ShouldReturnCreated()
    {
        // Arrange
        var command = new
        {
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                }
            },
            ShippingAddress = new
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange - Empty street violates Address Value Object invariants
        var command = new
        {
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                }
            },
            ShippingAddress = new
            {
                Street = "", // Invalid!
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithMixedCurrencies_ShouldReturnBadRequest()
    {
        // Arrange - Different currencies in Money Value Objects
        var command = new
        {
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                },
                new
                {
                    ProductName = "Product 2",
                    Quantity = 1,
                    Price = 30.00m,
                    Currency = "EUR" // Different currency!
                }
            },
            ShippingAddress = new
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrder_WithStronglyTypedId_ShouldReturnCorrectOrder()
    {
        // Arrange - Create an order first
        var createCommand = new
        {
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new
                {
                    ProductName = "Test Product",
                    Quantity = 3,
                    Price = 100.00m,
                    Currency = "USD"
                }
            },
            ShippingAddress = new
            {
                Street = "456 Oak Ave",
                City = "Boston",
                State = "MA",
                PostalCode = "02101",
                Country = "USA"
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Get the order using strongly-typed ID
        var response = await _client.GetAsync($"/api/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(orderId);
        order.Items.Should().HaveCount(1);
        order.Total.Should().Be(300.00m); // 3 * 100
        order.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task CreateOrder_ShouldEnforceDomainInvariants()
    {
        // Arrange - Negative price violates Money Value Object invariants
        var command = new
        {
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new
                {
                    ProductName = "Product",
                    Quantity = 1,
                    Price = -50.00m, // Invalid!
                    Currency = "USD"
                }
            },
            ShippingAddress = new
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private class OrderDto
    {
        public Guid Id { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public decimal Total { get; set; }
        public string Currency { get; set; } = string.Empty;
        public AddressDto ShippingAddress { get; set; } = new();
    }

    private class OrderItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    private class AddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
