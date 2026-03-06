using DDD.Sales.Application.Commands;
using DDD.Sales.Domain.Aggregates;
using DDD.Sales.Domain.Repositories;
using DDD.Sales.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DDD.Sales.Application.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _customerRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOrder()
    {
        // Arrange
        var customerId = CustomerId.Create();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId.Value,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                }
            },
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        var customer = Customer.Create(
            customerId,
            Email.Create("customer@example.com"),
            "John Doe");

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orderRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        _orderRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Order>(o =>
                o.CustomerId.Value == customerId.Value &&
                o.Items.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        _orderRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentCustomer_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                }
            },
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Customer not found");

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMultipleCurrencies_ShouldReturnFailure()
    {
        // Arrange
        var customerId = CustomerId.Create();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId.Value,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                },
                new()
                {
                    ProductName = "Product 2",
                    Quantity = 1,
                    Price = 30.00m,
                    Currency = "EUR" // Different currency!
                }
            },
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        var customer = Customer.Create(
            customerId,
            Email.Create("customer@example.com"),
            "John Doe");

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("All items must have the same currency");

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidMultipleItems_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var customerId = CustomerId.Create();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId.Value,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                },
                new()
                {
                    ProductName = "Product 2",
                    Quantity = 3,
                    Price = 30.00m,
                    Currency = "USD"
                }
            },
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        var customer = Customer.Create(
            customerId,
            Email.Create("customer@example.com"),
            "John Doe");

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _orderRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        _orderRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Order>(o =>
                o.Items.Count == 2 &&
                o.Total.Amount == 190.00m && // (2 * 50) + (3 * 30) = 190
                o.Total.Currency == Currency.USD),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidAddress_ShouldReturnFailure()
    {
        // Arrange
        var customerId = CustomerId.Create();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId.Value,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductName = "Product 1",
                    Quantity = 2,
                    Price = 50.00m,
                    Currency = "USD"
                }
            },
            ShippingAddress = new AddressDto
            {
                Street = "", // Invalid - empty street
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        var customer = Customer.Create(
            customerId,
            Email.Create("customer@example.com"),
            "John Doe");

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<CustomerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
