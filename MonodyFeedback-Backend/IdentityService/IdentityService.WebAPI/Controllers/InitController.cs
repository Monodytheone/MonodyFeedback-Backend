using CommonInfrastructure.Filters.JWTRevoke;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Zack.JWT;

namespace IdentityService.WebAPI.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class InitController : ControllerBase
{
    private readonly IIdentityRepository _repository;
    private readonly UserManager<User> _userManager;
    //private readonly IdUserManager _userManager;

    private readonly RoleManager<Role> _roleManager;
    private readonly HttpContext _httpContext;
    private readonly IOptionsSnapshot<JWTOptions> _jwtOptions;

    public InitController(RoleManager<Role> roleManager, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor, IOptionsSnapshot<JWTOptions> jwtOptions, IIdentityRepository repository)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _httpContext = httpContextAccessor.HttpContext;
        _jwtOptions = jwtOptions;
        _repository = repository;
    }

    [HttpPost]
    [Authorize(Roles = "master")]
    public async Task<ActionResult<string>> InitIdentity()
    {
        // 创建三个Role
        if (await _roleManager.RoleExistsAsync("master") == false)
        {
            Role role = new Role { Name = "master" };
            IdentityResult result = await _roleManager.CreateAsync(role);
            if (result.Succeeded == false)
            {
                return BadRequest("master Role创建失败");
            }
        }
        if (await _roleManager.RoleExistsAsync("processor") == false)
        {
            Role role = new Role { Name = "processor" };
            IdentityResult result = await _roleManager.CreateAsync(role);
            if (result.Succeeded == false)
            {
                return BadRequest("processor Role创建失败");
            }
        }
        if (await _roleManager.RoleExistsAsync("submitter") == false)
        {
            Role role = new Role { Name = "submitter" };
            IdentityResult result = await _roleManager.CreateAsync(role);
            if (result.Succeeded == false)
            {
                return BadRequest("submitter Role创建失败");
            }
        }

        // 创建用户名为"master"的账户
        User? masterUser = await _userManager.FindByNameAsync("master");
        if (masterUser == null)
        {
            masterUser = new User("master");
            IdentityResult result = await _userManager.CreateAsync(masterUser, "MonodFeb12138");
            if (result.Succeeded == false)
            {
                return BadRequest("master账号创建失败");
            }
        }
        // 赋予master角色
        if (await _userManager.IsInRoleAsync(masterUser, "master")  == false)
        {
            IdentityResult result = await _userManager.AddToRoleAsync(masterUser, "master");
            if (result.Succeeded == false)
            {
                return BadRequest("赋予master角色失败");
            }
        }

        return Ok();
    }

    [HttpPost]
    [Authorize(Roles = "master")]
    public async Task<ActionResult> GenerateProcessorAccount(string processorName, string password)
    {
        await _repository.CreateProcessorAsync(processorName, password);
        // 错误都抛了错，由异常筛选器报500
        return Ok();
    }

    [HttpPost]
    [Authorize(Roles = "master")]
    public async Task<ActionResult> UnlockAccount(string userNameOrId, bool useId)
    {
        User user;
        if (useId)
        {
            user = await _userManager.FindByIdAsync(userNameOrId);
        }
        else
        {
            user = await _userManager.FindByNameAsync(userNameOrId);
        }
        if (user == null) 
        { 
            return NotFound("未找到用户");
        }
        await _userManager.SetLockoutEndDateAsync(user, null).CheckIdentityResultAsync();
        await _userManager.ResetAccessFailedCountAsync(user).CheckIdentityResultAsync();
        return Ok();
    }

    [HttpDelete]
    [Authorize(Roles = "master")]
    public async Task<ActionResult> DeleteUser(Guid userId)
    {
        User? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound("要删除的用户不存在");
        }
        IdentityResult result = await _userManager.DeleteAsync(user);
        if (result.Succeeded == false)
        {
            return BadRequest(result.Errors);
        }
        else
        {
            return Ok("删除成功");
        }
    }

    [HttpGet]
    [Authorize(Roles = "submitter")]
    public string GetJWTPayload()
    {
        string id = _httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        string userName = this.User.FindFirst(ClaimTypes.Name)!.Value;
        string[] roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray();
        string jwtVersion = User.FindFirst("JWTVersion")!.Value;
        return $"Id = {id}\nUserName = {userName}\nrole = {roles[0]}\nJWTVersion = {jwtVersion}";
    }

    [HttpGet]
    [Authorize]
    public ActionResult IsLogin()
    {
        return Ok("okokokk");
    }

    [HttpGet]
    public string GetJWTOptions()
    {
        string ret = $"key = {_jwtOptions.Value.Key}\nexpire = {_jwtOptions.Value.ExpireSeconds}\nIssuer = {_jwtOptions.Value.Issuer}";
        return ret;
    }
}
