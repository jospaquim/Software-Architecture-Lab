using FluentValidation;
using EDA.API.Controllers;

namespace EDA.API.Validators;

public class AddItemRequestValidator : AbstractValidator<AddItemRequest>
{
    public AddItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required")
            .NotEqual(Guid.Empty).WithMessage("ProductId cannot be empty GUID");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required")
            .MaximumLength(200).WithMessage("ProductName cannot exceed 200 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("UnitPrice must be greater than 0")
            .LessThanOrEqualTo(1_000_000).WithMessage("UnitPrice cannot exceed 1,000,000");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(10_000).WithMessage("Quantity cannot exceed 10,000");
    }
}
