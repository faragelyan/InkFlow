using FluentValidation;
using InkFlow.Application.Features.Articles.DTOs.Requests;

namespace InkFlow.Application.Features.Articles.Validators;

public class CreateArticleValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleValidator()
    {
        RuleFor(request => request.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title cannot exceed 250 characters.");

        RuleFor(request => request.Subtitle)
            .MaximumLength(500).WithMessage("Subtitle cannot exceed 500 characters.");

        RuleFor(request => request.Content)
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .WithMessage("Content is required.");

        RuleFor(request => request.CoverImageUrl)
            .MaximumLength(500).WithMessage("Cover image URL cannot exceed 500 characters.")
            .Must(BeAbsoluteHttpUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.CoverImageUrl))
            .WithMessage("Cover image URL must be an absolute HTTP or HTTPS URL.");

        RuleFor(request => request.TopicIds)
            .NotNull().WithMessage("Topic IDs cannot be null.")
            .Must(ids => ids is not null && ids.Distinct().Count() <= 3).WithMessage("An article cannot have more than 3 topics.");

        RuleFor(request => request.TagIds)
            .NotNull().WithMessage("Tag IDs cannot be null.")
            .Must(ids => ids is not null && ids.Distinct().Count() <= 10).WithMessage("An article cannot have more than 10 tags.");
    }

    private static bool BeAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}