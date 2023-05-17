using FAQService.Domain.Entities;

namespace FAQService.Domain;

public interface IFAQRepository
{
    Task<int> ComputeMaxSequenceOfTabsAsync();

    void InsertTabIntoDb(Tab tab);

    Task<bool> CheckExistedTabNameAsync(string tabName);

    Task<IEnumerable<Guid>> GetTabIdsInDbAsync();

    Task<Tab?> GetTabAsync(Guid tabId);

    Task<Page?> GetPageAsync(Guid pageId);

    Task<Guid[]> GetSortedTabIdsAfterDeleteAsync();

    Task<Guid[]> GetSortedPageIdsAfterDeletePageAsync(Guid tabId);
}
