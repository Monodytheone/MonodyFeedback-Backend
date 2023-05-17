using FluentValidation;

namespace FAQService.WebAPI.Controllers.Requests;

public record CreateQandARequest(Guid PageId, string Question, string Answer);

public class CreateQandARequestValidator : AbstractValidator<CreateQandARequest>
{
    public CreateQandARequestValidator()
    {
        RuleFor(r => r.Question).NotEmpty();
        RuleFor(r => r.Answer).NotEmpty();
    }
}