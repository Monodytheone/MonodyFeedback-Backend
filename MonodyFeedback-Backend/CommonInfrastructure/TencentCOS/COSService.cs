using CommonInfrastructure.TencentCOS.Responses;
using COSSTS;
using COSXML.Auth;
using COSXML.Model.Tag;
using COSXML;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace CommonInfrastructure.TencentCOS;

public class COSService
{
    public TempCredentialResponse GeneratePutObjectTempCredential(string bucket, string region, string secretId, string secretKey, string allowPrefix, int durationSeconds)
    {
        string[] allowActions = new string[]  // 允许的操作范围
        {
            "name/cos:PutObject",
        };

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

        Dictionary<string, object> credential = STSClient.genCredential(values);

        JObject creds = JObject.FromObject(credential["Credentials"]);
        TmpCredential tmpCredential = new(creds["Token"].ToString(), creds["TmpSecretId"].ToString(), creds["TmpSecretKey"].ToString());
        string expiredTime = credential["ExpiredTime"].ToString()!;
        string expiration = credential["Expiration"].ToString()!;
        string requestId = credential["RequestId"].ToString()!;
        string startTime = credential["StartTime"].ToString()!;
        return new TempCredentialResponse(tmpCredential, expiredTime, expiration, requestId, startTime);
    }

    /// <summary>
    /// 生成预签名Url
    /// </summary>
    /// <param name="durationSeconds">签名有效时长（秒）</param>
    /// <param name="objectKeys"></param>
    /// <returns></returns>
    public List<string> GeneratePreSignatureAvatarUrls(string appid, string region, string secretId, string secretKey, long durationSeconds, string bucket, params string[] objectKeys)
    {
        // 初始化 CosXmlConfig:
        CosXmlConfig config = new CosXmlConfig.Builder()
              .IsHttps(true)  //设置默认 HTTPS 请求
              .SetRegion(region)  //设置一个默认的存储桶地域
              .SetDebugLog(false)  //显示日志
              .Build();  //创建 CosXmlConfig 对象

        // 初始化QCloudCredentialProvider:
        QCloudCredentialProvider cosCredentialProvider = new DefaultQCloudCredentialProvider(secretId, secretKey, durationSeconds);

        // 初始化 CosXmlServer
        CosXml cosXml = new CosXmlServer(config, cosCredentialProvider);

        List<string> avatarUrls = new();
        foreach (string objectKey in objectKeys)
        {
            PreSignatureStruct preSignatureStruct = new()
            {
                appid = appid,  // APPID
                region = region,  // 存储桶所在地域
                bucket = bucket,// 存储桶名称，格式必须为 bucketname-APPID
                key = objectKey,  // 对象键
                httpMethod = "GET",  // HTTP请求方法
                isHttps = true,
                signDurationSecond = 60,  // 请求签名时间
                headers = null,  // 签名中需要校验的header
                queryParameters = null
            };
            avatarUrls.Add(cosXml.GenerateSignURL(preSignatureStruct));
        }
        
        return avatarUrls;
    }
}
