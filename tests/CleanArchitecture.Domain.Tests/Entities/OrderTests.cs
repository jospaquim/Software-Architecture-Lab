using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CleanArchitecture.Domain.Tests.Entities;

public class OrderTests
{
    private static Product CreateTestProduct(int id = 1, string name = "Test Product", decimal price = 50.00m, int stock = 100)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Price = price,
            Stock = stock,
            Sku = $"SKU-{id:D3}",
            Description = "Test product",
            IsActive = true
        };
    }

    private static Order CreateTestOrder(int customerId = 1)
    {
        return new Order
        {
            CustomerId = customerId,
            OrderNumber = "ORD-001",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.CreditCard
        };
    }

    [Fact]
    public void NewOrder_ShouldHavePendingStatus()
    {
        var order = CreateTestOrder();

        order.Should().NotBeNull();
        order.CustomerId.Should().Be(1);
        order.OrderNumber.Should().Be("ORD-001");
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().BeEmpty();
        order.Uid.Should().NotBeEmpty();
    }

    [Fact]
    public void AddItem_ShouldAddItemAndCalculateTotal()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();

        order.AddItem(product, 2, 50.00m);

        order.Items.Should().HaveCount(1);
        order.Subtotal.Should().Be(100.00m);
    }

    [Fact]
    public void AddItem_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();

        Action act = () => order.AddItem(product, -1, 10.00m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void AddItem_WithNegativePrice_ShouldThrowArgumentException()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();

        Action act = () => order.AddItem(product, 1, -10.00m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unit price cannot be negative*");
    }

    [Fact]
    public void ConfirmOrder_ShouldChangeStatusToProcessing()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();
        order.AddItem(product, 1, 10.00m);

        order.ConfirmOrder();

        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public void ConfirmOrder_WithoutItems_ShouldThrowInvalidOperationException()
    {
        var order = CreateTestOrder();

        Action act = () => order.ConfirmOrder();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot confirm order without items*");
    }

    [Fact]
    public void Ship_ShouldChangeStatusToShipped()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();
        order.AddItem(product, 1, 10.00m);
        order.ConfirmOrder();
        order.ProcessPayment();

        order.Ship();

        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedDate.Should().NotBeNull();
    }

    [Fact]
    public void Ship_WithoutConfirming_ShouldThrowInvalidOperationException()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();
        order.AddItem(product, 1, 10.00m);

        Action act = () => order.Ship();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();
        order.AddItem(product, 1, 10.00m);

        order.Cancel("Customer requested");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.Notes.Should().Contain("Customer requested");
    }

    [Fact]
    public void Cancel_WhenAlreadyShipped_ShouldThrowInvalidOperationException()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();
        order.AddItem(product, 1, 10.00m);
        order.ConfirmOrder();
        order.ProcessPayment();
        order.Ship();

        Action act = () => order.Cancel("Too late");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel order after it has been shipped*");
    }

    [Fact]
    public void AddMultipleItems_ShouldCalculateSubtotalCorrectly()
    {
        var order = CreateTestOrder();
        var product1 = CreateTestProduct(id: 1, name: "Product 1", price: 10.00m);
        var product2 = CreateTestProduct(id: 2, name: "Product 2", price: 15.00m);
        var product3 = CreateTestProduct(id: 3, name: "Product 3", price: 5.00m);

        order.AddItem(product1, 2, 10.00m);  // 20
        order.AddItem(product2, 1, 15.00m);  // 15
        order.AddItem(product3, 3, 5.00m);   // 15

        order.Items.Should().HaveCount(3);
        order.Subtotal.Should().Be(50.00m);
    }

    [Fact]
    public void AddItem_SameProduct_ShouldIncreaseQuantity()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();

        order.AddItem(product, 2, 50.00m);
        order.AddItem(product, 3, 50.00m);

        order.Items.Should().HaveCount(1);
        order.Items.First().Quantity.Should().Be(5);
        order.Subtotal.Should().Be(250.00m);
    }

    [Fact]
    public void AddItem_WhenShipped_ShouldThrowInvalidOperationException()
    {
        var order = CreateTestOrder();
        var product = CreateTestProduct();
        order.AddItem(product, 1, 10.00m);
        order.ConfirmOrder();
        order.ProcessPayment();
        order.Ship();

        var newProduct = CreateTestProduct(id: 2, name: "New Product");

        Action act = () => order.AddItem(newProduct, 1, 20.00m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot modify order after it has been shipped*");
    }

    [Fact]
    public void GenerateOrderNumber_ShouldReturnFormattedString()
    {
        var orderNumber = Order.GenerateOrderNumber();

        orderNumber.Should().StartWith("ORD-");
    }
}
