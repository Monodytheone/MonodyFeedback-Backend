using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Zack.JWT;

namespace IdentityService.Domain;

public class IdentityDomainService
{
    private readonly IIdentityRepository _repository;
    private readonly TokenService _tokenService;
    private readonly IOptionsSnapshot<JWTOptions> _jwtOptions;

    public IdentityDomainService(IIdentityRepository repository, TokenService tokenService, IOptionsSnapshot<JWTOptions> jwtOptions)
    {
        _repository = repository;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions;
    }

    public async Task<SignUpStatus> SingUpAsync(string userName, string password)
    {
        // 检查用户名是否可用
        if(await _repository.CheckUserNameUsabilityAsync(userName) == false)
        {
            return SignUpStatus.UserNameUnavailable;
        }

        IdentityResult result = await _repository.CreateSubmitterAsync(userName, password);
        if(result.Succeeded)
        {
            return SignUpStatus.Successful;
        }
        else
        {
            return SignUpStatus.Failed;
        }
    }

    public async Task<(SignInResult, string?)> LoginAsync(string userName, string password)
    {
        User? user = await _repository.FindUserByUserNameAsync(userName);
        if(user == null)
        {
            return (SignInResult.Failed, null);
        }
        SignInResult signInResult = await _repository.CheckForLoginAsync(user, password);
        if (signInResult.Succeeded)
        {
            await _repository.UpdateJWTVersionAsync(user);  // 使其他JWT失效，实现单客户端登录
            string token = await this.BuildTokenAsync(user);
            return (signInResult, token);
        }
        else
        {
            return (signInResult, null);
        }
    }

    // 虽然只是方法调用的转发，但更改用户头像显然属于核心业务逻辑，故仍写在领域服务中
    public Task ChangeAvatarObjectKeyAsync(string userId, string avatarObjectKey)
    {
        return _repository.ChangeAvatarObjectKeyAsync(userId, avatarObjectKey);
    }

    private async Task<string> BuildTokenAsync(User user)
    {
        IList<string> roles = await _repository.GetRolesOfUserAsync(user);
        List<Claim> claims = new();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, user.UserName));
        claims.Add(new Claim("JWTVersion", user.JWTVersion.ToString()));
        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return _tokenService.BuildToken(claims, _jwtOptions.Value);
    }
}
