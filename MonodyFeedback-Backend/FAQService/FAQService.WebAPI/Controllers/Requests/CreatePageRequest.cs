using FluentValidation;

namespace FAQService.WebAPI.Controllers.Requests;

public record CreatePageRequest(Guid TabId, string Title, bool IsPureQandA, bool IsHot, string? HtmlUrl);

public class CreatePageRequestValidator : AbstractValidator<CreatePageRequest>
{
    public CreatePageRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().Length(1, 50);
    }
}