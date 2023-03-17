namespace CommonInfrastructure.TencentCOS.Responses;

/// <summary>
/// 临时密钥响应体（JavaScript SDK就吃這个）
/// </summary>
public record TempCredentialResponse(TmpCredential Credentials, string ExpiredTime, string Expiration, string RequestId, string StartTime);

public record TmpCredential(string SessionToken, string TmpSecretId, string TmpSecretKey);
