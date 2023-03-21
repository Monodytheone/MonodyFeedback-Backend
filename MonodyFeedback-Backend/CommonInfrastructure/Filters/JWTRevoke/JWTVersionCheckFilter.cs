using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CommonInfrastructure.Filters.JWTRevoke;

public class JWTVersionCheckFilter : IAsyncActionFilter
{
    private readonly IJWTVersionTool _jwtVersionTool;

    public JWTVersionCheckFilter(IJWTVersionTool jwtVersionTool)
    {
        _jwtVersionTool = jwtVersionTool;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 检查是否标注了NotCheckJwtAttribute，是则跳过本filter
        bool hasNotCheckJwtAttribute = false;
        if (context.ActionDescriptor is ControllerActionDescriptor)
        {
            ControllerActionDescriptor actionDesc = (ControllerActionDescriptor)context.ActionDescriptor;
            hasNotCheckJwtAttribute = actionDesc.MethodInfo.IsDefined(typeof(NotCheckJWTAttribute), true);
        }
        if (hasNotCheckJwtAttribute)
        {
            await next();
            return;
        }

        Claim? claimJWTVersion = context.HttpContext.User.FindFirst("JWTVersion");
        if (claimJWTVersion == null)  // 若payload中没有JWTVersion则报错返回
        {
            context.Result = new ObjectResult("登录状态失效，请重新登录") { StatusCode = 401 };
            return;
        }

        long clientJWTVersion = Convert.ToInt64(claimJWTVersion.Value);  // 从JWT取到的JWTVersion
        string userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        long serverJWTVersion = await _jwtVersionTool.GetServerJWTVersionAsync(userId);

        // 若服务端的JWTVersion大于客户端传来的JWTVersion，就说明客户端的JWT已经失效了
        if (serverJWTVersion > clientJWTVersion)
        {
            context.Result = new ObjectResult("登录状态失效，请重新登录") { StatusCode = 401 };
            return;
        }
        else
        {
            await next();
        }
    }
}
