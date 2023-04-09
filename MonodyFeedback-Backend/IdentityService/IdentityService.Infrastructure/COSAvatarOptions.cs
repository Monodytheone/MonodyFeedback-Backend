//namespace CommonInfrastructure.TencentCOS;
namespace IdentityService.Infrastructure;

public class COSAvatarOptions
{
    public string AppId { get; set; }

    public string SecretId { get; set; }

    public string SecretKey { get; set; }

    public string Bucket { get; set; }

    public string Region { get; set; }

    /// <summary>
    /// 头像所处的文件夹名称
    /// </summary>
    public string AvatarFolder { get; set; }
}
