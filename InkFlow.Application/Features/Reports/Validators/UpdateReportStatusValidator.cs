using FluentValidation;
using InkFlow.Application.Features.Reports.DTOs.Requests;
using InkFlow.Domain;

namespace InkFlow.Application.Features.Reports.Validators;

public class UpdateReportStatusValidator : AbstractValidator<UpdateReportStatusRequest>
{
    public UpdateReportStatusValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum()
            .WithMessage("Report status is invalid.");

        RuleFor(request => request.Status)
            .NotEqual(ReportStatus.Pending)
            .WithMessage("A report cannot be moved back to pending.");
    }
}
