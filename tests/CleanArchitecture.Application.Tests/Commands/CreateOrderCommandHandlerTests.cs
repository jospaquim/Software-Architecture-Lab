using CleanArchitecture.Application.Commands.Orders;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchitecture.Application.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = productId, Quantity = 2, UnitPrice = 50.00m }
            }
        };

        var customer = Customer.Create("test@example.com", "John", "Doe", "123 Main St", "555-0100");
        var product = Product.Create("Product", "Description", 50.00m, "SKU-001", 100);

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().NotBe(Guid.Empty);

        _orderRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Order>(o =>
                o.CustomerId == customerId &&
                o.Items.Count == 1 &&
                o.TotalAmount == 100.00m),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentCustomer_ShouldReturnFailure()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 50.00m }
            }
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Customer not found");

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ShouldReturnFailure()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = productId, Quantity = 1, UnitPrice = 50.00m }
            }
        };

        var customer = Customer.Create("test@example.com", "John", "Doe", "123 Main St", "555-0100");

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Product not found");

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInsufficientStock_ShouldReturnFailure()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = productId, Quantity = 200, UnitPrice = 50.00m }
            }
        };

        var customer = Customer.Create("test@example.com", "John", "Doe", "123 Main St", "555-0100");
        var product = Product.Create("Product", "Description", 50.00m, "SKU-001", 100); // Only 100 in stock

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient stock");

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Order must have at least one item");
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ShouldCreateOrderWithCorrectTotal()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = product1Id, Quantity = 2, UnitPrice = 50.00m },
                new() { ProductId = product2Id, Quantity = 3, UnitPrice = 30.00m }
            }
        };

        var customer = Customer.Create("test@example.com", "John", "Doe", "123 Main St", "555-0100");
        var product1 = Product.Create("Product1", "Description", 50.00m, "SKU-001", 100);
        var product2 = Product.Create("Product2", "Description", 30.00m, "SKU-002", 100);

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);

        _productRepositoryMock
            .Setup(x => x.GetByIdAsync(product2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);

        _unitOfWorkMock
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
                o.TotalAmount == 190.00m), // (2 * 50) + (3 * 30) = 190
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
