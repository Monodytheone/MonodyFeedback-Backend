using CommonInfrastructure.Filters.JWTRevoke;
using COSXML.Network;
using FAQService.Domain;
using FAQService.WebAPI.Controllers.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Runtime.InteropServices;
using TencentCloud.Cpdp.V20190820.Models;
using Zack.ASPNETCore;

namespace FAQService.WebAPI.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class AccessController : ControllerBase
{
    private readonly IFAQRepository _repository;
    private readonly IMemoryCacheHelper _memoryCacheHelper;
    private readonly RepositoryForAccess _accessRepository;

    public AccessController(IFAQRepository repository, IMemoryCacheHelper memoryCacheHelper, RepositoryForAccess accessRepository)
    {
        _repository = repository;
        _memoryCacheHelper = memoryCacheHelper;
        _accessRepository = accessRepository;
    }

    /// <summary>
    /// 获取所有的TabName和每个Tab中所有问题的标题（用于在主页展示）
    /// </summary>
    [NotCheckJWT]
    [HttpGet]
    [ResponseCache(Duration = 5)]  // 启用响应缓存，包在内存缓存外面
    public async Task<List<TabVM>> GetFAQInfoForHomePage()
    {
        List<TabVM> tabVMs = (await _memoryCacheHelper.GetOrCreateAsync("TabVMsForHomePage", (cacheEntry) =>
        {
            return _accessRepository.GetFAQInfoForHomePageAsync()!;
        }, 5))!;
        return tabVMs;
    }

    [NotCheckJWT]
    [HttpGet("{pageId}")]
    [ResponseCache(Duration = 5)]
    public async Task<GetPageResponse> GetPage(Guid pageId)
    {
        GetPageResponse response = await _memoryCacheHelper.GetOrCreateAsync($"Page{pageId}", cacheEntry =>
        {
            return _accessRepository.GetPageAsync(pageId);
        }, 5);
        return response;
    }


}
