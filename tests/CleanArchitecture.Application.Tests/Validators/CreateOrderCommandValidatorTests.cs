using CleanArchitecture.Application.Commands.Orders;
using CleanArchitecture.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CleanArchitecture.Application.Tests.Validators;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator;

    public CreateOrderCommandValidatorTests()
    {
        _validator = new CreateOrderCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.Empty,
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Validate_WithEmptyItems_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WithNullItems_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = null!
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WithInvalidQuantity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 0, // Invalid
                    UnitPrice = 50.00m
                }
            }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNegativeQuantity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = -5,
                    UnitPrice = 50.00m
                }
            }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNegativePrice_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    UnitPrice = -50.00m
                }
            }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyProductId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = Guid.Empty,
                    Quantity = 2,
                    UnitPrice = 50.00m
                }
            }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithMultipleValidItems_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    UnitPrice = 50.00m
                },
                new()
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 3,
                    UnitPrice = 30.00m
                }
            }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
