using FluentValidation;
using InkFlow.Application.Features.Topics.DTOs.Requests;

namespace InkFlow.Application.Features.Topics.Validators;

public class CreateTopicValidator : AbstractValidator<CreateTopicRequest>
{
    public CreateTopicValidator()
    {
        RuleFor(x => x.Name)
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Topic name is required.")
            .MaximumLength(100).WithMessage("Topic name cannot exceed 100 characters.");
    }
}
