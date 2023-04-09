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


    private Picture() { }

    public Picture(/*Paragraph paragraph,*/ string bucket, string region, string fullObjectKey)
    {
        Id = Guid.NewGuid();
        //Paragraph = paragraph;
        Bucket = bucket;
        Region = region;
        FullObjectKey = fullObjectKey;
    }
}
