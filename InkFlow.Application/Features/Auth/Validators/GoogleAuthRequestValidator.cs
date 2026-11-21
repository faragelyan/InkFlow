using FluentValidation;
using InkFlow.Application.Features.Auth.DTOs.Requests;

namespace InkFlow.Application.Features.Auth.Validators;

public class GoogleAuthRequestValidator : AbstractValidator<GoogleAuthRequest>
{
    public GoogleAuthRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token is required.");
    }
}
