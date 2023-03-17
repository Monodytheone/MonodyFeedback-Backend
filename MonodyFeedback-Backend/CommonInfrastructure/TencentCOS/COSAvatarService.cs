using CommonInfrastructure.TencentCOS.Responses;
using COSSTS;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Tag;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace CommonInfrastructure.TencentCOS;

public class COSAvatarService
{
    private readonly IOptionsSnapshot<COSAvatarOptions> _cosOptions;

    public COSAvatarService(IOptionsSnapshot<COSAvatarOptions> cosOptions)
    {
        _cosOptions = cosOptions;
    }

    /// <summary>
    /// 生成往COS上传图片的临时密钥
    /// </summary>
    /// <param name="allowPrefix">允许上传到的路径前缀</param>
    /// <param name="durationSeconds">密钥有效时长</param>
    /// <returns></returns>
    public TempCredentialResponse GeneratePutPictureTempCredential(string allowPrefix, int durationSeconds)
    {
        string bucket = _cosOptions.Value.Bucket;
        string region = _cosOptions.Value.Region;
        string[] allowActions = new string[]  // 允许的操作范围
        {
            "name/cos:PutObject",
        };
        string secretId = _cosOptions.Value.SecretId;
        string secretKey = _cosOptions.Value.SecretKey;

        Dictionary<string, object> values = new()
        {
            { "bucket", bucket },
            { "region", region },
            { "allowPrefix", allowPrefix },
            { "allowActions", allowActions },
            { "durationSeconds", durationSeconds },  // 临时密钥有效时长
            { "secretId", secretId },
            { "secretKey", secretKey },
        };

        Dictionary<string ,object> credential = STSClient.genCredential(values);

        JObject creds = JObject.FromObject(credential["Credentials"]);
        TmpCredential tmpCredential = new(creds["Token"].ToString(), creds["TmpSecretId"].ToString(), creds["TmpSecretKey"].ToString());
        string expiredTime = credential["ExpiredTime"].ToString()!;
        string expiration = credential["Expiration"].ToString()!;
        string requestId = credential["RequestId"].ToString()!;
        string startTime = credential["StartTime"].ToString()!;
        return new TempCredentialResponse(tmpCredential, expiredTime, expiration, requestId, startTime);
    }

    public string GeneratePreSignatureAvatarUrl(string objectKey)
    {
        // 初始化 CosXmlConfig:
        string appid = _cosOptions.Value.AppId;
        string region = _cosOptions.Value.Region;
        CosXmlConfig config = new CosXmlConfig.Builder()
              .IsHttps(true)  //设置默认 HTTPS 请求
              .SetRegion(region)  //设置一个默认的存储桶地域
              .SetDebugLog(false)  //显示日志
              .Build();  //创建 CosXmlConfig 对象

        // 初始化QCloudCredentialProvider:
        string secretId = _cosOptions.Value.SecretId;
        string secretKey = _cosOptions.Value.SecretKey;
        long durationSecond = 60;  // 每次请求签名有效时长，单位为秒
        QCloudCredentialProvider cosCredentialProvider = new DefaultQCloudCredentialProvider(secretId, secretKey, durationSecond);

        // 初始化 CosXmlServer
        CosXml cosXml = new CosXmlServer(config, cosCredentialProvider);


        PreSignatureStruct preSignatureStruct = new()
        {
            appid = appid,  // APPID
            region = region,  // 存储桶所在地域
            bucket = _cosOptions.Value.Bucket,// 存储桶名称，格式必须为 bucketname-APPID
            key = objectKey,  // 对象键
            httpMethod = "GET",  // HTTP请求方法
            isHttps = true,
            signDurationSecond = 60,  // 请求签名时间
            headers = null,  // 签名中需要校验的header
            queryParameters = null
        };

        return cosXml.GenerateSignURL(preSignatureStruct);
    }
}
