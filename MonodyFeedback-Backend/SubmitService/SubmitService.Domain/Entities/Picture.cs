namespace SubmitService.Domain.Entities;

public class Picture
{
    public Guid Id { get; init; }

    public Paragraph Paragraph { get; init; }

    /// <summary>
    /// 存储桶名称（不要跟某个存储桶彻底耦死）
    /// </summary>
    public string Bucket { get; init; }

    /// <summary>
    /// 存储桶所属地域
    /// </summary>
    public string Region { get; init; }

    /// <summary>
    /// 完整的对象键
    /// </summary>
    public string FullObjectKey { get; init; }

    /// <summary>
    /// 在Paragraph中的序号
    /// </summary>
    public byte Sequence { get; init; }


    private Picture() { }

    public Picture(string bucket, string region, string fullObjectKey, byte sequence)
    {
        //Id = Guid.NewGuid();
        Bucket = bucket;
        Region = region;
        FullObjectKey = fullObjectKey;
        Sequence = sequence;
    }
}
