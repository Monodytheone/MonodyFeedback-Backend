using FluentValidation;

namespace FAQService.WebAPI.Controllers.Requests;

public record SortQandAsInPageRequest(Guid PageId, Guid[] SortedQandAIds);

public class SortQandAsInPageRequestValidator : AbstractValidator<SortQandAsInPageRequest>
{
    public SortQandAsInPageRequestValidator()
    {
        RuleFor(r => r.PageId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(r => r.SortedQandAIds)
            .NotEmpty()
            .NotContains(Guid.Empty)
            .NotDuplicated().WithMessage("有重复Id");
    }
}