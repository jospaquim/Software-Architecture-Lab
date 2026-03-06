using FluentValidation;
using EDA.API.Controllers;

namespace EDA.API.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required")
            .NotEqual(Guid.Empty).WithMessage("CustomerId cannot be empty GUID");
    }
}
