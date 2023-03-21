using CommonInfrastructure.Filters.JWTRevoke;
using CommonInfrastructure.TencentCOS;
using CommonInfrastructure.TencentCOS.Responses;
using FluentValidation;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.WebAPI.Controllers.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;

namespace IdentityService.WebAPI.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class IdentityController : ControllerBase
{
    private readonly IIdentityRepository _repository;
    private readonly IdentityDomainService _domainService;
    private readonly COSAvatarService _avatarService;
    private readonly IOptionsSnapshot<COSAvatarOptions> _avatarOptions;

    // Validators of FluentValidation:
    private readonly IValidator<SignUpRequest> _signUpValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ChangeSubmitterPasswordWithJWTRequest> _changeSubmitterPasswordValidator;
    private readonly IValidator<ChangeSubmitterPasswordWithUserNameRequest> _changeSubmitterPasswordWithUserNameValidator;

    public IdentityController(IIdentityRepository repository, IdentityDomainService domainService, IValidator<SignUpRequest> signUpValidator, COSAvatarService avatarService, IOptionsSnapshot<COSAvatarOptions> avatarOptions, IValidator<ChangeSubmitterPasswordWithJWTRequest> changeSubmitterPasswordValidator, IValidator<LoginRequest> loginValidator, IValidator<ChangeSubmitterPasswordWithUserNameRequest> changeSubmitterPasswordWithUserNameValidator)
    {
        _repository = repository;
        _domainService = domainService;
        _signUpValidator = signUpValidator;
        _avatarService = avatarService;
        _avatarOptions = avatarOptions;
        _changeSubmitterPasswordValidator = changeSubmitterPasswordValidator;
        _loginValidator = loginValidator;
        _changeSubmitterPasswordWithUserNameValidator = changeSubmitterPasswordWithUserNameValidator;
    }

    /// <summary>
    /// 检查用户名是否可用
    /// <para>不区分大小写，如Yoimiya已存在则yoiMIYa不可用，因为UserManager的FindByName方法不区分大小写</para>
    /// </summary>
    /// <param name="userName"></param>
    /// <returns>true: 可用；false：不可用</returns>
    [HttpGet]
    [NotCheckJWT]
    public async Task<bool> CheckUserNameUsability(string userName)
    {
        if (userName.Length < 3 || userName.Length > 18)
        {
            return false;
        }
        return await _repository.CheckUserNameUsabilityAsync(userName);
    }

    [HttpPost]
    [NotCheckJWT]
    public async Task<ActionResult<string>> SignUp(SignUpRequest request)
    {
        // FluentValidation的手动校验模式：
        var validationResult = await _signUpValidator.ValidateAsync(request);
        if(validationResult.IsValid  == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        SignUpStatus signUpStatus = await _domainService.SingUpAsync(request.UserName, request.Password);
        if (signUpStatus == SignUpStatus.Successful)
        {
            return Ok("注册成功");
        }
        else if (signUpStatus == SignUpStatus.Failed)
        {
            return BadRequest("注册失败");
        }
        else
        {
            return BadRequest("用户名已占用");
        }
    }

    [HttpPost]
    [NotCheckJWT]
    public async Task<ActionResult<string>> Login(LoginRequest request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        (Microsoft.AspNetCore.Identity.SignInResult result, string? token) 
            = await _domainService.LoginAsync(request.UserName, request.Password);
        if (result.Succeeded)
        {
            return token!;
        }
        else if (result.IsLockedOut)
        {
            return StatusCode((int)HttpStatusCode.Locked, "账号已锁定，请稍后再试");
        }
        else
        {
            return StatusCode((int)HttpStatusCode.BadRequest, "用户名或密码错误");
        }
    }

    [HttpPost]
    [Authorize(Roles = "submitter")]  // 只有普通用户可以通过旧密码设置新密码
    public async Task<ActionResult> ChangeSubmitterPasswordWithJWT(ChangeSubmitterPasswordWithJWTRequest request)
    {
        var validationResult = await _changeSubmitterPasswordValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        string userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isSuccessful = await _domainService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (isSuccessful)
        {
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    [HttpPost]
    [NotCheckJWT]
    public async Task<ActionResult> ChangeSubmitterPasswordWithUserName(ChangeSubmitterPasswordWithUserNameRequest request)
    {
        var validationResult = await _changeSubmitterPasswordWithUserNameValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        User? user = await _repository.FindUserByUserNameAsync(request.UserName);
        if (user == null)
        {
            return NotFound("用户名不存在");
        }

        // 只有普通用户可以通过旧密码设置新密码
        if (await _repository.ConfirmUserNotProcessorOrMaster(user) == false)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, "该账户不允许自行重置密码，请联系管理员");
        }

        bool isSuccessful = await _domainService.ChangePasswordAsync(user.Id.ToString(), request.CurrentPassword, request.NewPassword);
        if (isSuccessful)
        {
            return Ok();
        }
        else
        {
            return BadRequest();
        }        
    }

    // 幂等的更新所以用put
    [HttpPut]
    [Authorize]
    public async Task<ActionResult> ChangeAvatarObjectKey(string newObjectKey)
    {
        // 异常由异常筛选器处理，这里不用try catch
        string userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _domainService.ChangeAvatarObjectKeyAsync(userId, newObjectKey);
        return Ok();
    }

    [HttpGet]
    [Authorize]
    public ActionResult<TempCredentialResponse> AskChangeAvatarTempCredential()
    {       
        string userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        string objectKey = $"{_avatarOptions.Value.AvatarFolder}/{userId}.png";
        TempCredentialResponse response = _avatarService.GeneratePutPictureTempCredential(objectKey, 60);
        return Ok(response);      
    }

    /// <summary>
    /// 获取预签名的头像Url
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<string>> GetPreSignatureAvatarUrl()
    {       
        string userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        string avatarurl = await _repository.GetAvatarUrlAsync(userId);
        return Ok(avatarurl);
    }

    /// <summary>
    /// 为了其他服务校验JWTVersion时获取服务端JWTVersion而提供的API
    /// <para>图省事不设身份校验了，即使你通过这个接口拿到JWTVersion了又能怎样呢，你又拼不进JWT里</para>
    /// </summary>
    [HttpGet]
    [NotCheckJWT]
    public Task<long> GetServerJWTVersion(string userId)
    {
        return _repository.GetJWTVersionAsync(userId);
    }
}
