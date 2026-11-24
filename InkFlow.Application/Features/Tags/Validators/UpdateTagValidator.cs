using FluentValidation;
using InkFlow.Application.Features.Tags.DTOs.Requests;

namespace InkFlow.Application.Features.Tags.Validators;

public class UpdateTagValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagValidator()
    {
        RuleFor(x => x.Name)
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Tag name is required.")
            .MaximumLength(50).WithMessage("Tag name cannot exceed 50 characters.");
    }
}
