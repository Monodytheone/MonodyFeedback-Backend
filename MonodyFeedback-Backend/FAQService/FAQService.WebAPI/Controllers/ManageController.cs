using FAQService.Domain;
using FAQService.Domain.Entities;
using FAQService.Infrastructure;
using FAQService.WebAPI.Controllers.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zack.ASPNETCore;

namespace FAQService.WebAPI.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = "master")]
[UnitOfWork(typeof(FAQDbContext))]
public class ManageController : ControllerBase
{
    private readonly FAQDomainService _domainService;
    private readonly FAQDbContext _dbContext;

    // Validators of FluentValidation:
    private readonly IValidator<CreatePageRequest> _createPageValidator;
    private readonly IValidator<CreateQandARequest> _createQandAValidator;
    private readonly IValidator<ModifyQandARequest> _modifyQandAValidator;
    private readonly IValidator<SortTabsRequest> _sortTabsValidator;
    private readonly IValidator<SortPagesInTabRequest> _sortPagesInTabValidator;
    private readonly IValidator<SortQandAsInPageRequest> _sortQandAsInPageValidator;

    public ManageController(FAQDomainService domainService, IValidator<CreatePageRequest> createPageValidator, FAQDbContext dbContext, IValidator<CreateQandARequest> createQandAValidator, IValidator<ModifyQandARequest> modifyQandAValidator, IValidator<SortTabsRequest> sortTabsValidator, IValidator<SortPagesInTabRequest> sortPagesInTabValidator, IValidator<SortQandAsInPageRequest> sortQandAsInPageValidator)
    {
        _domainService = domainService;
        _createPageValidator = createPageValidator;
        _dbContext = dbContext;
        _createQandAValidator = createQandAValidator;
        _modifyQandAValidator = modifyQandAValidator;
        _sortTabsValidator = sortTabsValidator;
        _sortPagesInTabValidator = sortPagesInTabValidator;
        _sortQandAsInPageValidator = sortQandAsInPageValidator;
    }

    [HttpPost]
    public async Task<ActionResult> CreateTab(string tabName)
    {
        if(tabName.Length > 8)
        {
            return BadRequest("标签长度不得大于8");
        }
        await _domainService.CreateTabAsync(tabName);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> CreatePage(CreatePageRequest request)
    {
        var validationResult = await _createPageValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        if(request.IsPureQandA && request.HtmlUrl != null)
        {
            return BadRequest("内容为纯Q&A的Page，不得指定htmlUrl");
        }
        if (request.IsPureQandA == false && request.HtmlUrl == null)
        {
            return BadRequest("内容为外部Html的Page，必须指定htmlUrl");
        }

        Tab? tab = await _dbContext.Tabs
            .Include(tab => tab.Pages)
            .FirstOrDefaultAsync(tab => tab.Id == request.TabId);
        if(tab == null)
        {
            return BadRequest("创建Page时，tabId不存在");
        }

        if (request.IsPureQandA)
        {
            _domainService.CreatePureQandAPage(tab, request.IsHot, request.Title);
            return Ok();
        }
        else
        {
            _domainService.CreateHtmlPage(tab, request.IsHot, request.Title, request.HtmlUrl!);
            return Ok();
        }
    }

    [HttpPost]
    public async Task<ActionResult> CreateQandA(CreateQandARequest request)
    {
        var validationResult = await _createQandAValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        Page? page = await _dbContext.Pages
            .Include(page => page.QandAs)
            .FirstOrDefaultAsync(page => page.Id == request.PageId);
        if(page == null)
        {
            return BadRequest("创建Q&A时，pageId不存在");
        }
        if (page.IsPureQandA == false)
        {
            return BadRequest("不得为内容为外部Html的Page添加Q&A");
        }

        _domainService.CreateQandA(page, request.Question, request.Answer);
        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> ChangeTabName(Guid tabId, string newName)
    {
        if(newName.Length > 8)
        {
            return BadRequest("标签长度不得大于8");
        }

        Tab? tab = await _dbContext.Tabs.FirstOrDefaultAsync(tab => tab.Id == tabId);
        if(tab == null)
        {
            return BadRequest("修改TabName时，TabId不存在");
        }

        bool result = await _domainService.ChangeTabNameAsync(tab, newName);
        if (result == false)
        {
            return BadRequest($"标签“{newName}”已存在");
        }
        else return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> ChangePageTitle(Guid pageId, string newTitle)
    {
        Page? page = await _dbContext.Pages.FirstOrDefaultAsync(page => page.Id == pageId);
        if (page == null)
        {
            return BadRequest($"修改PageTitle时，PageId = {pageId} 不存在");
        }
        _domainService.ChangePageTitle(page, newTitle);
        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> ChangeHotStatusOfPage(Guid pageId, bool isHot)
    {
        Page? page = await _dbContext.Pages.FirstOrDefaultAsync(page => page.Id == pageId);
        if (page == null)
        {
            return BadRequest($"修改Page的Hot状态时，PageId = {pageId} 不存在");
        }

        page.ChangeIsHot(isHot);
        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> ChangePageContentToPureQandA(Guid pageId)
    {
        Page? page = await _dbContext.Pages
            .FirstOrDefaultAsync(page => page.Id == pageId);
        if (page == null)
        {
            return BadRequest($"试图把Page内容由外部Html改为纯Q&A时，PageId = {pageId} 不存在");
        }
        if (page.IsPureQandA)
        {
            return BadRequest($"pageId = {pageId} 已经是纯Q&A的了");
        }

        page.ToPureQandA();
        return Ok();
    }


    [HttpPut]
    public async Task<ActionResult> ChangePageContentToHtml(Guid pageId, string htmlUrl)
    {
        Page? page = await _dbContext.Pages
            .Include(page => page.QandAs)
            .FirstOrDefaultAsync(page => page.Id == pageId);
        if (page == null)
        {
            return BadRequest($"试图把Page内容由纯Q&A改为外部Html时，PageId = {pageId} 不存在");
        }
        if (page.IsPureQandA == false)
        {
            return BadRequest($"pageId = {pageId} 的内容已经是html了");
        }

        page.ToHtml(htmlUrl);
        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> ChangePageHtmlUrl(Guid pageId, string newUrl)
    {
        Page? page = await _dbContext.Pages
            .FirstOrDefaultAsync (page => page.Id == pageId);
        if (page == null)
        {
            return BadRequest($"试图更改页面的HtmlUrl时，PageId = {pageId} 不存在");
        }
        if (page.IsPureQandA)
        {
            return BadRequest($"不得对纯Q&A页面 PageId = {pageId} 设置HtmlUrl");
        }
        page.ToHtml(newUrl);
        return Ok();
    }

    [HttpPut]
    public async Task<ActionResult> ModifyQandA(ModifyQandARequest request)
    {
        var validationResult = await _modifyQandAValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        QandA? qandA = await _dbContext.QandAs.FirstOrDefaultAsync(q => q.Id == request.QandAId);
        if (qandA == null)
        {
            return BadRequest($"修改Q&A时，Q&AId = {request.QandAId} 不存在");
        }

        qandA.ChangeQuestion(request.Question).ChangeAnswer(request.Answer);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SortTabs(SortTabsRequest request)
    {
        var validationResult = await _sortTabsValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        await _domainService.SortTabsAsync(request.SortedTabIds);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SortPagesInTab(SortPagesInTabRequest request)
    {
        var validationResult = await _sortPagesInTabValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        Tab? tab = await _dbContext.Tabs
            .Include(tab => tab.Pages)
            .FirstOrDefaultAsync(tab => tab.Id == request.TabId);
        if (tab == null)
        {
            return BadRequest($"为Page排序时，TabId = {request.TabId} 不存在");
        }

        await _domainService.SortPagesInTabAsync(tab, request.SortedPageIds);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SortQandAsInPage(SortQandAsInPageRequest request)
    {
        var validationResult = await _sortQandAsInPageValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        Page? page = await _dbContext.Pages
            .Include(page => page.QandAs)
            .FirstOrDefaultAsync(page => page.Id == request.PageId);
        if(page == null)
        {
            return BadRequest($"为Q&A排序时，PageId = {request.PageId} 不存在");
        }
        if (page.IsPureQandA == false)
        {
            return BadRequest("不可对内容为外部Html的Page中的Q&A进行排序");
        }

        page.SortQandAs(request.SortedQandAIds);
        return Ok();
    }

    [HttpPost("{tabId}")]
    public async Task<ActionResult> DeleteTab(Guid tabId)
    {
        Tab? tab = await _dbContext.Tabs.FirstOrDefaultAsync(tab => tab.Id == tabId);
        if (tab == null)
        {
            return BadRequest($"删除Tab时，tabId = {tabId} 不存在");
        }

        _dbContext.Tabs.Remove(tab);
        await _dbContext.SaveChangesAsync();
        await _domainService.SortTabsAfterDeleteAsync();
        return Ok();
    }

    [HttpPost("{pageId}")]
    public async Task<ActionResult> DeletePage(Guid pageId)
    {
        Page? page = await _dbContext.Pages
            .Include(page => page.Tab)
            .FirstOrDefaultAsync(page => page.Id == pageId);
        if (page == null)
        {
            return BadRequest($"删除Page时，pageId = {pageId} 不存在");
        }

        _dbContext.Pages.Remove(page);
        await _dbContext.SaveChangesAsync();
        await _domainService.SortPagesInTabAfterDeleteAsync(page.Tab);
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> DeleteQandA(Guid pageId, Guid qandAId)
    {
        Page? page = await _dbContext.Pages
            .Include(page => page.QandAs)
            .FirstOrDefaultAsync(page => page.Id == pageId);
        if (page == null)
        {
            return BadRequest($"pageId = {pageId} 不存在");
        }
        if (page.IsPureQandA == false)
        {
            return BadRequest($"pageId = {pageId} 的内容为外部Html");
        }
        QandA? qandA = page.QandAs.FirstOrDefault(qandA => qandA.Id == qandAId);
        if(qandA == null)
        {
            return BadRequest($"Q&AId = {qandAId} 不存在");
        }

        page.RemoveQandA(qandAId);
        return Ok();
    }
}
