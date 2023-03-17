using FluentValidation;
using IdentityService.Domain;
using System.Text.RegularExpressions;

namespace IdentityService.WebAPI.Controllers.Requests;

public record SignUpRequest(string UserName, string Password);

public class SignUpRequestValidator : AbstractValidator<SignUpRequest>
{
    public SignUpRequestValidator(IIdentityRepository repository)
    {
        RuleFor(r => r.UserName).NotEmpty().Length(3, 18).WithMessage("用户名应为3-18个字符")
            .MustAsync(async (userName, ct) => await repository.CheckUserNameUsabilityAsync(userName) == true)
            .WithMessage("用户名已被占用");
        RuleFor(r => r.Password).NotEmpty().Length(8, 18).WithMessage("密码应为8-18个字符")
            .Must(pwd => Regex.IsMatch(pwd, "^[\x21-\x7e]+$")).WithMessage("密码只能由字母、数字、英文符号组成")
            .Must(pwd => Regex.IsMatch(pwd, "[a-z]")).WithMessage("密码必须含有小写字母")
            .Must(pwd => Regex.IsMatch(pwd, "[0-9]")).WithMessage("密码必须包含数字");
    }
}