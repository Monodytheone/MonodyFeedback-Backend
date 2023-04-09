using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace SubmitService.Submit.WebAPI.Controllers.Requests;

public record SupplementRequest([RequiredGuid]Guid SubmissionId, string TextContent, List<PictureInfo> PictureInfos);

public class SupplementRequestValidator : AbstractValidator<SupplementRequest>
{
    public SupplementRequestValidator()
    {
        RuleFor(x => x.TextContent).NotEmpty().MinimumLength(5).MaximumLength(1000);
        RuleFor(x => x.PictureInfos).Must(pictureInfos => pictureInfos.Count <= 10).WithMessage("最多十张图片");
    }
}