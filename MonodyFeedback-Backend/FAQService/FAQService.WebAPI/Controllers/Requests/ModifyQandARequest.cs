using FluentValidation;

namespace FAQService.WebAPI.Controllers.Requests;

public record ModifyQandARequest(Guid QandAId, string Question, string Answer);

public class ModifyQandARequestValidator : AbstractValidator<ModifyQandARequest>
{
    public ModifyQandARequestValidator()
    {
        RuleFor(r => r.Question).NotEmpty();
        RuleFor(r => r.Answer).NotEmpty();
    }
}