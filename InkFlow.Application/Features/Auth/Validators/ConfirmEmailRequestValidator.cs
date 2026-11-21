using FluentValidation;
using InkFlow.Application.Features.Auth.DTOs.Requests;

namespace InkFlow.Application.Features.Auth.Validators;

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Confirmation token is required.");
    }
}
