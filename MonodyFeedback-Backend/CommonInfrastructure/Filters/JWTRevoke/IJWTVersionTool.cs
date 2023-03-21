namespace CommonInfrastructure.Filters.JWTRevoke;

/// <summary>
/// 用于JWTVersionCheckFilter获取服务端JWTVersion
/// </summary>
public interface IJWTVersionTool
{
    Task<long> GetServerJWTVersionAsync(string userGuid, CancellationToken ct = default);
}
