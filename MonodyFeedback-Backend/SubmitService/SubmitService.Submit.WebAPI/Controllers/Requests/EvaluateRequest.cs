using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace SubmitService.Submit.WebAPI.Controllers.Requests;

public record EvaluateRequest([RequiredGuid]Guid SubmissionId, bool IsSolved, byte Grade);

public class EvaluateRequestValidator : AbstractValidator<EvaluateRequest>
{
    public EvaluateRequestValidator()
    {
        RuleFor(r => r.IsSolved).NotNull();
        RuleFor(r => r.Grade).NotNull()
            .Must(grade => grade <= 5 && grade >= 1).WithMessage("评分需在[1, 5]之间");
    }
}