using FluentValidation;
using InkFlow.Application.Features.Topics.DTOs.Requests;

namespace InkFlow.Application.Features.Topics.Validators;

public class UpdateTopicValidator : AbstractValidator<UpdateTopicRequest>
{
    public UpdateTopicValidator()
    {
        RuleFor(x => x.Name)
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Topic name is required.")
            .MaximumLength(100).WithMessage("Topic name cannot exceed 100 characters.");
    }
}
