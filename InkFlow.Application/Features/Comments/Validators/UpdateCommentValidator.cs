using FluentValidation;
using InkFlow.Application.Features.Comments.DTOs.Requests;

namespace InkFlow.Application.Features.Comments.Validators;

public class UpdateCommentValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentValidator()
    {
        RuleFor(request => request.Content)
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .WithMessage("Comment content is required.")
            .MaximumLength(2000)
            .WithMessage("Comment content cannot exceed 2000 characters.");
    }
}