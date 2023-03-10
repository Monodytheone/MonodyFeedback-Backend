using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain.Entities;

public class User : IdentityUser<Guid>
{
    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime CreationTime { get; init; }

    /// <summary>
    /// 头像在存储服务中的对象键
    /// </summary>
    public string? AvatarObjectKey { get; private set; }

    public User(string userName) : base(userName)
    {
        Id = Guid.NewGuid();
        CreationTime = DateTime.Now;
    }


    public void ChangeAvatar(string newAvatarObjectKey)
    {
        AvatarObjectKey = newAvatarObjectKey;
    }
}
