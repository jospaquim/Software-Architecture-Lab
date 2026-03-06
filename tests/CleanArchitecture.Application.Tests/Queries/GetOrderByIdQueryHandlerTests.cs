using AutoMapper;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Queries.Orders;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CleanArchitecture.Application.Tests.Queries;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _mapperMock = new Mock<IMapper>();
        _handler = new GetOrderByIdQueryHandler(_orderRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_ShouldReturnOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var query = new GetOrderByIdQuery { OrderId = orderId };

        var order = Order.Create(customerId, "ORD-001");
        order.AddItem(Guid.NewGuid(), "Product", 2, 50.00m);

        var orderDto = new OrderDto
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            CustomerId = customerId,
            TotalAmount = 100.00m,
            Items = new List<OrderItemDto>
            {
                new() { ProductName = "Product", Quantity = 2, UnitPrice = 50.00m }
            }
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mapperMock
            .Setup(x => x.Map<OrderDto>(order))
            .Returns(orderDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(orderId);
        result.Data.OrderNumber.Should().Be("ORD-001");
        result.Data.TotalAmount.Should().Be(100.00m);
        result.Data.Items.Should().HaveCount(1);

        _orderRepositoryMock.Verify(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentOrder_ShouldReturnFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var query = new GetOrderByIdQuery { OrderId = orderId };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Order not found");
        result.Data.Should().BeNull();

        _orderRepositoryMock.Verify(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.Verify(x => x.Map<OrderDto>(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetOrderByIdQuery { OrderId = Guid.Empty };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid order ID");

        _orderRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
