using FluentValidation;

namespace FAQService.WebAPI.Controllers.Requests;

public record SortTabsRequest(Guid[] SortedTabIds);

public class SortTabsRequestValidator : AbstractValidator<SortTabsRequest>
{
    public SortTabsRequestValidator()
    {
        RuleFor(r => r.SortedTabIds)
            .NotEmpty()
            .NotContains(Guid.Empty)
            .NotDuplicated().WithMessage("有重复Id");
    }
}