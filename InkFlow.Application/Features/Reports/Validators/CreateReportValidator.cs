using FluentValidation;
using InkFlow.Application.Features.Reports.DTOs.Requests;

namespace InkFlow.Application.Features.Reports.Validators;

public class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator()
    {
        RuleFor(request => request.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("Report reason is required.")
            .MaximumLength(1000)
            .WithMessage("Report reason cannot exceed 1000 characters.");

        RuleFor(request => request)
            .Must(request => request.ArticleId.HasValue ^ request.CommentId.HasValue)
            .WithMessage("A report must target exactly one article or comment.");
    }
}
