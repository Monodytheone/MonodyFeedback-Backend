using CommonInfrastructure.TencentCOS;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IdentityService.Infrastructure;

public class IdentityRepository : IIdentityRepository
{
    private readonly UserManager<User> _userManager;
    private readonly COSService _cosService;
    private readonly IOptionsSnapshot<COSAvatarOptions> _cosAvatarOptions;

    public IdentityRepository(UserManager<User> userManager, COSService cosService, IOptionsSnapshot<COSAvatarOptions> cosAvatarOptions)
    {
        _userManager = userManager;
        _cosService = cosService;
        _cosAvatarOptions = cosAvatarOptions;
    }

    public async Task ChangeAvatarObjectKeyAsync(string userId, string avatarObjectKey)
    {
        // 正常情况是不会出错的，所以直接抛错，报500
        User user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("更改用户头像对象键时用户不存在");
        }
        user.ChangeAvatar(avatarObjectKey);
        await _userManager.UpdateAsync(user).CheckIdentityResultAsync();
    }

    public async Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        IdentityResult result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await _userManager.SetLockoutEndDateAsync(user, null).CheckIdentityResultAsync();
            await _userManager.ResetAccessFailedCountAsync(user).CheckIdentityResultAsync();
        }
        return result.Succeeded;
    }

    public async Task<SignInResult> CheckForLoginAsync(User user, string password)
    {
        if (await _userManager.IsLockedOutAsync(user))
        {
            return SignInResult.LockedOut;
        }

        if (await _userManager.CheckPasswordAsync(user, password) == true)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
            return SignInResult.Success;
        }
        else
        {
            _ = await _userManager.AccessFailedAsync(user);
            return SignInResult.Failed;
        }
    }

    public async Task<bool> CheckUserNameUsabilityAsync(string userName)
    {
        User user = await _userManager.FindByNameAsync(userName);
        if(user == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task<bool> ConfirmUserNotProcessorOrMaster(User user)
    {
        bool isProcessor = await _userManager.IsInRoleAsync(user, "processor");
        bool isMaster = await _userManager.IsInRoleAsync(user, "master");
        if (isProcessor || isMaster)
        {
            return false;
        }
        else return true;
    }

    public async Task<IdentityResult> CreateSubmitterAsync(string userName, string password)
    {
        User user = new(userName);
        IdentityResult result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded == false)
        {
            return result;
        }
        result = await _userManager.AddToRoleAsync(user, "submitter");
        return result;
    }

    public Task<User?> FindUserByIdAsync(string userId)
    {
        return _userManager.FindByIdAsync(userId)!; 
    }

    public Task<User?> FindUserByUserNameAsync(string userName)
    {
        return _userManager.FindByNameAsync(userName)!;
    }

    public async Task CreateProcessorAsync(string processorName, string password)
    {
        User? user = await _userManager.FindByNameAsync(processorName);
        if (user != null)
        {
            throw new Exception("用户名已存在");  // 扔给异常筛选器
        }
        user = new User(processorName);
        await _userManager.CreateAsync(user, password).CheckIdentityResultAsync();
        if (await _userManager.IsInRoleAsync(user, "processor") == false)
        {
            await _userManager.AddToRoleAsync(user, "processor").CheckIdentityResultAsync();
        }
    }

    public async Task<string> GetAvatarUrlAsync(string userId, long durationSeconds)
    {
        User user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            // 异常自有异常筛选器处理，故这里写明原因也无所谓
            throw new Exception("生成头像预签名Url时用户不存在");
        }
        string avatarObjectKey = user.AvatarObjectKey;
        COSAvatarOptions avatarOptions = _cosAvatarOptions.Value;
        string avatarUrl = _cosService.GeneratePreSignatureAvatarUrls(avatarOptions.AppId, avatarOptions.Region, avatarOptions.SecretId, avatarOptions.SecretKey, durationSeconds, avatarOptions.Bucket, avatarObjectKey)[0];
        return avatarUrl;
    }

    public async Task<long> GetJWTVersionAsync(string userId)
    {
        User? user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("为了其他服务获取服务端JWTVersion时用户不存在");
        }
        return user.JWTVersion;
    }

    public Task<IList<string>> GetRolesOfUserAsync(User user)
    {
        return _userManager.GetRolesAsync(user);
    }

    public Task UpdateJWTVersionAsync(User user)
    {
        user.UpdateJWTVersion();
        return _userManager.UpdateAsync(user);
    }
}
