using FluentValidation;
using InkFlow.Application.Features.Authors.DTOs.Requests;

namespace InkFlow.Application.Features.Authors.Validators;

public class UpdateAuthorProfileValidator : AbstractValidator<UpdateAuthorProfileRequest>
{
    public UpdateAuthorProfileValidator()
    {
        RuleFor(request => request.DisplayName)
            .Must(displayName => !string.IsNullOrWhiteSpace(displayName) && displayName.Trim().Length >= 3)
            .WithMessage("Display name must be at least 3 non-whitespace characters.")
            .MaximumLength(50).WithMessage("Display name cannot exceed 50 characters.");

        RuleFor(request => request.Bio)
            .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters.");

        RuleFor(request => request.ProfileImageUrl)
            .MaximumLength(500).WithMessage("Profile image URL cannot exceed 500 characters.")
            .Must(BeAbsoluteHttpUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ProfileImageUrl))
            .WithMessage("Profile image URL must be an absolute HTTP or HTTPS URL.");
    }

    private static bool BeAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}