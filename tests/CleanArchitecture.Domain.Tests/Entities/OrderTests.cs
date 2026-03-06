using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CleanArchitecture.Domain.Tests.Entities;

/// <summary>
/// Tests unitarios para la entidad Order
/// Validan la lógica de negocio encapsulada en la entidad
/// </summary>
public class OrderTests
{
    [Fact]
    public void Create_ShouldCreateOrderWithPendingStatus()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderNumber = "ORD-001";

        // Act
        var order = Order.Create(customerId, orderNumber);

        // Assert
        order.Should().NotBeNull();
        order.CustomerId.Should().Be(customerId);
        order.OrderNumber.Should().Be(orderNumber);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(0);
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyOrderNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderNumber = string.Empty;

        // Act
        Action act = () => Order.Create(customerId, orderNumber);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Order number cannot be empty*");
    }

    [Fact]
    public void AddItem_ShouldAddItemAndCalculateTotal()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        var productId = Guid.NewGuid();
        var productName = "Test Product";
        var quantity = 2;
        var unitPrice = 50.00m;

        // Act
        order.AddItem(productId, productName, quantity, unitPrice);

        // Assert
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(100.00m); // 2 * 50
    }

    [Fact]
    public void AddItem_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        var productId = Guid.NewGuid();
        var quantity = -1;

        // Act
        Action act = () => order.AddItem(productId, "Product", quantity, 10.00m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void AddItem_WithNegativePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        var productId = Guid.NewGuid();
        var unitPrice = -10.00m;

        // Act
        Action act = () => order.AddItem(productId, "Product", 1, unitPrice);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unit price must be greater than zero*");
    }

    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WithoutItems_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");

        // Act
        Action act = () => order.Confirm();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot confirm an empty order*");
    }

    [Fact]
    public void Ship_ShouldChangeStatusToShipped()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);
        order.Confirm();

        // Act
        order.Ship();

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_WithoutConfirming_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);

        // Act
        Action act = () => order.Ship();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot ship an order that is not confirmed*");
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyShipped_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);
        order.Confirm();
        order.Ship();

        // Act
        Action act = () => order.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel a shipped order*");
    }

    [Fact]
    public void AddMultipleItems_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");

        // Act
        order.AddItem(Guid.NewGuid(), "Product 1", 2, 10.00m);  // 20
        order.AddItem(Guid.NewGuid(), "Product 2", 1, 15.00m);  // 15
        order.AddItem(Guid.NewGuid(), "Product 3", 3, 5.00m);   // 15

        // Assert
        order.Items.Should().HaveCount(3);
        order.TotalAmount.Should().Be(50.00m); // 20 + 15 + 15
    }

    [Fact]
    public void SetShippingAddress_ShouldSetAddress()
    {
        // Arrange
        var order = Order.Create(Guid.NewGuid(), "ORD-001");
        var address = new Address("123 Main St", "City", "State", "Country", "12345");

        // Act
        order.SetShippingAddress(address);

        // Assert
        order.ShippingAddress.Should().NotBeNull();
        order.ShippingAddress!.Street.Should().Be("123 Main St");
    }
}
