using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CommonInfrastructure.Filters.JWTRevoke;

/// <summary>
/// 为IdentityService之外的其他服务提供的获取服务端JWTVersion的实现
/// <para>获取途径为：向IdentityService提供的接口发Http请求</para>
/// </summary>
public class JWTVersionToolForOtherServices : IJWTVersionTool
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public JWTVersionToolForOtherServices(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<long> GetServerJWTVersionAsync(string userGuid, CancellationToken ct = default)
    {
        string requestUrl = $"{_configuration["ServerJWTVersionUrl"]}?userId={userGuid}";
        HttpClient httpClient = _httpClientFactory.CreateClient();
        string response = await httpClient.GetStringAsync(requestUrl, ct);
        return long.Parse(response);
    }
}
