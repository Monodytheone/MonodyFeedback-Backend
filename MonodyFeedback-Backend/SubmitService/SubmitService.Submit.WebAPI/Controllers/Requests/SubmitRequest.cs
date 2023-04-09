using FluentValidation;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;

namespace SubmitService.Submit.WebAPI.Controllers.Requests;
public record SubmitRequest(string? TelNumber, string? Email, string TextContent, List<PictureInfo> PictureInfos);

public class SubmitRequestValidator : AbstractValidator<SubmitRequest>
{
    public SubmitRequestValidator()
    {
        RuleFor(r => r.TelNumber).Must(telNumber => telNumber.IsNullOrEmpty() || Regex.IsMatch(telNumber, "^1(3\\d|4[5-9]|5[0-35-9]|6[567]|7[0-8]|8\\d|9[0-35-9])\\d{8}$")).WithMessage("手机号格式不正确");  // 其实前端已经校验过了
        RuleFor(r => r.Email).Must(email => email.IsNullOrEmpty() || Regex.IsMatch(email, "^\\w+@[a-zA-Z_]+?\\.[a-zA-Z]{2,3}$")).WithMessage("邮箱格式不正确");  // 其实前端已经校验过了
        RuleFor(r => r.TextContent)
            .NotEmpty().WithMessage("文字内容不可为空")
            .Length(10, 1000).WithMessage("文字长度需在[10, 1000]间");
        RuleFor(r => r.PictureInfos).Must(picInfos => picInfos.Count <= 10).WithMessage("最多十张图片");
    }
}