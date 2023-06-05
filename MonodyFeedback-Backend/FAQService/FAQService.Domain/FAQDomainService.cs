using FAQService.Domain.Entities;
using System.Diagnostics;

namespace FAQService.Domain;

public class FAQDomainService
{
    private readonly IFAQRepository _repository;

    public FAQDomainService(IFAQRepository repository)
    {
        _repository = repository;
    }

    public async Task CreateTabAsync(string tabName)
    {
        int maxSequence = await _repository.ComputeMaxSequenceOfTabsAsync();
        Tab tab = new(tabName, maxSequence + 1);
        _repository.InsertTabIntoDb(tab);
    }

    public void CreatePureQandAPage(Tab tab, bool isHot, string title)
    {
        //int pageNumInTab = await _repository.CountNumberOfPagesInTabAsync(tab);
        int pageNumInTab = tab.Pages.Count();
        Page page = new Page.Builder()
            .PureQandA(pageNumInTab + 1, isHot, title)
            .Build();
        tab.Pages.Add(page);
    }

    public void CreateHtmlPage(Tab tab, bool isHot, string title, string htmlUrl)
    {
        int pageNumInTab = tab.Pages.Count();
        Page page = new Page.Builder()
            .Html(pageNumInTab + 1, isHot, title, htmlUrl)
            .Build();
        tab.Pages.Add(page);
    }

    public void CreateQandA(Page page, string question, string answer)
    {
        int qandANumInPage = page.QandAs.Count();
        page.AddQandA(qandANumInPage + 1, answer, question);
    }

    public async Task<bool> ChangeTabNameAsync(Tab tab, string newName)
    {
        // 标签名称不得重复
        if (await _repository.CheckExistedTabNameAsync(newName) == true)
        {
            return false;
        }

        tab.ChangeName(newName);
        return true;
    }

    public void ChangePageTitle(Page page, string newTitle)
    {
        page.ChangeTitle(newTitle);
    }

    public async Task SortTabsAsync(Guid[] sortedTabIds)
    {
        IEnumerable<Guid> tabIdsInDb = await _repository.GetTabIdsInDbAsync();
        if (tabIdsInDb.SequenceIgnoreEqual(sortedTabIds) == false)
        {
            throw new Exception("为Tab排序时，Id无法一一对照");
        }

        int seqNum = 1;
        foreach (Guid tabId in sortedTabIds)
        {
            Tab? tab = await _repository.GetTabAsync(tabId);
            if (tab == null)
            {
                throw new Exception($"为Tab排序时，tabId = {tabId} 不存在");
                // 这里为什么要抛错：为了触发工作单元筛选器，不SaveChange到数据库
            }
            tab.ChangeSequence(seqNum);
            seqNum++;
        }
    }

    public async Task SortPagesInTabAsync(Tab tab, Guid[] sortedPageIds)
    {
        IEnumerable<Guid> pageIdsInDb = tab.Pages.Select(page => page.Id);
        foreach (Guid id in pageIdsInDb)
        {
            Console.WriteLine("InDb: " + id.ToString());
        }
        if (pageIdsInDb.SequenceIgnoreEqual(sortedPageIds) == false)
        {
            throw new Exception($"为tabId = {tab.Id} 中的Page排序时，Id无法一一对照");
        }

        int seqNum = 1;
        foreach (Guid pageId in sortedPageIds)
        {
            Page? page = await _repository.GetPageAsync(pageId);
            if (page == null)
            {
                throw new Exception($"为Page排序时，pageId = {pageId} 不存在");
            }
            page.ChangeSequence(seqNum);
            seqNum++;
        }
    }

    public async Task SortTabsAfterDeleteAsync()
    {
        Guid[] tabIdsAfterDelete = await _repository.GetSortedTabIdsAfterDeleteAsync();
        int seqNum = 1;
        foreach (Guid tabId in tabIdsAfterDelete)
        {
            Tab? tab = await _repository.GetTabAsync(tabId);
            if (tab == null)
            {
                throw new Exception($"删除Tab后排序时，tabId = {tabId} 不存在");
            }
            tab.ChangeSequence(seqNum);
            seqNum++;
        }
    }

    public async Task SortPagesInTabAfterDeleteAsync(Tab tab)
    {
        Guid[] pageIdsInTabAfterDeletePage = await _repository.GetSortedPageIdsAfterDeletePageAsync(tab.Id);
        int seqNum = 1;
        foreach (Guid pageId in pageIdsInTabAfterDeletePage)
        {
            Page? page = await _repository.GetPageAsync(pageId);
            if (page == null)
            {
                throw new Exception($"删除Page后排序时，pageId = {pageId} 不存在");
            }
            page.ChangeSequence(seqNum);
            seqNum++;
        }
    }
}
