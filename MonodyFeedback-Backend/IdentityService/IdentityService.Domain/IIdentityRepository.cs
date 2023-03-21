using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain;

public interface IIdentityRepository
{
    Task<bool> CheckUserNameUsabilityAsync(string userName);

    Task<IdentityResult> CreateSubmitterAsync(string userName, string password);

    Task<User?> FindUserByUserNameAsync(string userName);

    Task<User?> FindUserByIdAsync(string userId);

    Task<SignInResult> CheckForLoginAsync(User user, string password);

    Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword);

    Task<IList<string>> GetRolesOfUserAsync(User user);

    Task UpdateJWTVersionAsync(User user);

    Task ChangeAvatarObjectKeyAsync(string userId, string avatarObjectKey);

    Task<string> GetAvatarUrlAsync(string userId);

    Task<long> GetJWTVersionAsync(string userId);
}
