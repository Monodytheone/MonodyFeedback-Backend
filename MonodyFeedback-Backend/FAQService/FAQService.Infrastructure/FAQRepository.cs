using FAQService.Domain;
using FAQService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FAQService.Infrastructure;

public class FAQRepository : IFAQRepository
{
    private readonly FAQDbContext _dbContext;

    public FAQRepository(FAQDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CheckExistedTabNameAsync(string tabName)
    {
        return _dbContext.Tabs.AnyAsync(tab => tab.Name == tabName);
    }

    public Task<int> ComputeMaxSequenceOfTabsAsync()
    {
        return _dbContext.Tabs.CountAsync();
    }

    public Task<Page?> GetPageAsync(Guid pageId)
    {
        return _dbContext.Pages.FirstOrDefaultAsync(page => page.Id == pageId);
    }

    public Task<Guid[]> GetSortedPageIdsAfterDeletePageAsync(Guid tabId)
    {
        return _dbContext.Pages
             .Where(page => page.Tab.Id == tabId)
             .OrderBy(page => page.Sequence)
             .Select(page => page.Id)
             .ToArrayAsync();
    }

    public Task<Guid[]> GetSortedTabIdsAfterDeleteAsync()
    {
        return _dbContext.Tabs.OrderBy(tab => tab.Sequence).Select(tab => tab.Id).ToArrayAsync();
    }

    public Task<Tab?> GetTabAsync(Guid tabId)
    {
        return _dbContext.Tabs.FirstOrDefaultAsync(tab => tab.Id == tabId);
    }

    public async Task<IEnumerable<Guid>> GetTabIdsInDbAsync()
    {
        // 这里不await async一下，类型就对不上
        return await _dbContext.Tabs.Select(tab => tab.Id).ToArrayAsync();
    }

    public void InsertTabIntoDb(Tab tab)
    {
        _dbContext.Tabs.Add(tab);
    }
}
