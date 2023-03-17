using CommonInfrastructure.TencentCOS;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace IdentityService.Infrastructure;

public class IdentityRepository : IIdentityRepository
{
    private readonly UserManager<User> _userManager;
    private readonly COSAvatarService _avatarService;

    public IdentityRepository(UserManager<User> userManager, COSAvatarService avatarService)
    {
        _userManager = userManager;
        _avatarService = avatarService;
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
        IdentityResult result = await _userManager.UpdateAsync(user);
        if (result.Succeeded == false)
        {
            throw new Exception(JsonSerializer.Serialize(result.Errors));
        }
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

    public Task<User?> FindUserByUserNameAsync(string userName)
    {
        return _userManager.FindByNameAsync(userName)!;
    }

    public async Task<string> GetAvatarUrlAsync(string userId)
    {
        User user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            // 异常自有异常筛选器处理，故这里写明原因也无所谓
            throw new Exception("生成头像预签名Url时用户不存在");
        }
        string avatarObjectKey = user.AvatarObjectKey;
        return _avatarService.GeneratePreSignatureAvatarUrl(avatarObjectKey);
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
