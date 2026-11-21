using FluentValidation;
using InkFlow.Application.Features.Auth.DTOs.Requests;

namespace InkFlow.Application.Features.Auth.Validators;

public class RevokeTokenRequestValidator : AbstractValidator<RevokeTokenRequest>
{
    public RevokeTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
