using FluentValidation;

namespace IdentityService.WebAPI.Controllers.Requests;

public record LoginRequest(string UserName, string Password);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.UserName).NotEmpty();
        RuleFor(r => r.Password).NotEmpty();
    }
}