using CommonInfrastructure.Filters.JWTRevoke;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Infrastructure;

public class JWTVersionTool : IJWTVersionTool
{
    private readonly UserManager<User> _userManager;

    public JWTVersionTool(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<long> GetServerJWTVersionAsync(string userGuid, CancellationToken ct = default)
    {
        User? user = await _userManager.FindByIdAsync(userGuid);
        if (user == null)
        {
            throw new Exception("IdentityService获取服务端JWTVersion时竟然找不到用户");
        }

        return user.JWTVersion;
    }
}
