using FluentValidation;

namespace FAQService.WebAPI.Controllers.Requests;

public record SortPagesInTabRequest(Guid TabId, Guid[] SortedPageIds);

public class SortPagesInTabRequestValidator : AbstractValidator<SortPagesInTabRequest>
{
    public SortPagesInTabRequestValidator()
    {
        // 只进行初步的校验，把这个Id是否存在的检查放到Action里，这样代码可读性更强
        RuleFor(r => r.TabId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(r => r.SortedPageIds)
            .NotEmpty()
            .NotContains(Guid.Empty)
            .NotDuplicated().WithMessage("有重复Id");
    }
}