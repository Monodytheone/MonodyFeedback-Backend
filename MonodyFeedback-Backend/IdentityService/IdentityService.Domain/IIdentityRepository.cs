using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain;

public interface IIdentityRepository
{
    Task<bool> CheckUserNameUsabilityAsync(string userName);

    Task<IdentityResult> CreateSubmitterAsync(string userName, string password);

    Task<User?> FindUserByUserNameAsync(string userName);

    Task<SignInResult> CheckForLoginAsync(User user, string password);

    Task<IList<string>> GetRolesOfUserAsync(User user);

    Task UpdateJWTVersionAsync(User user);

    Task ChangeAvatarObjectKeyAsync(string userId, string avatarObjectKey);

    Task<string> GetAvatarUrlAsync(string userId);
}
