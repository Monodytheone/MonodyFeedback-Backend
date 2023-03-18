using FluentValidation;
using System.Text.RegularExpressions;

namespace IdentityService.WebAPI.Controllers.Requests;

public record ChangeSubmitterPasswordWithJWTRequest(string CurrentPassword, string NewPassword);

public class ChangeSubmitterPasswordWithJWTRequestValidator : AbstractValidator<ChangeSubmitterPasswordWithJWTRequest>
{
    public ChangeSubmitterPasswordWithJWTRequestValidator()
    {
        RuleFor(r => r.CurrentPassword).NotEmpty();
        RuleFor(r => r.NewPassword).NotEmpty().Length(8, 18).WithMessage("密码应为8-18个字符")
            .Must(pwd => Regex.IsMatch(pwd, "^[\x21-\x7e]+$")).WithMessage("密码只能由字母、数字、英文符号组成")
            .Must(pwd => Regex.IsMatch(pwd, "[a-z]")).WithMessage("密码必须含有小写字母")
            .Must(pwd => Regex.IsMatch(pwd, "[0-9]")).WithMessage("密码必须包含数字");
    }
}