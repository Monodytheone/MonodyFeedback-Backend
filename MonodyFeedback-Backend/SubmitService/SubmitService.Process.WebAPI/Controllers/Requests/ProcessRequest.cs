using FluentValidation;
using SubmitService.Domain.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace SubmitService.Process.WebAPI.Controllers.Requests;

public record ProcessRequest([RequiredGuid]Guid SubmissionId, SubmissionStatus NextStatus, string TextContent, List<PictureInfo> PictureInfos);

public record PictureInfo(string BucketName, string Region, string FullObjectKey);

public class ProcessRequestValidator : AbstractValidator<ProcessRequest>
{
    public ProcessRequestValidator()
    {
        RuleFor(x => x.NextStatus).NotNull()
            .Must(status => status == SubmissionStatus.ToBeEvaluated 
                || status == SubmissionStatus.ToBeSupplemented).WithMessage("下一步状态不合规，仅能变为“待评价”或“待完善”");
        RuleFor(x => x.TextContent).NotEmpty().MinimumLength(5).MaximumLength(1000);
        RuleFor(x => x.PictureInfos).Must(pictureInfos => pictureInfos.Count <= 10).WithMessage("最多十张图片");
    }
}