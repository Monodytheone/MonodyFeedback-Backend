using FAQService.Domain.Entities;
using FAQService.Infrastructure;
using FAQService.WebAPI.Controllers.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using TencentCloud.Cpdp.V20190820.Models;

namespace FAQService.WebAPI;

public class RepositoryForAccess
{
    private readonly FAQDbContext _dbContext;

    public RepositoryForAccess(FAQDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TabVM>> GetFAQInfoForHomePageAsync()
    {
        return _dbContext.Tabs
            .AsNoTracking()
            .Include(tab => tab.Pages)
            .OrderBy(tab => tab.Sequence)
            .Select(tab => new TabVM(
                tab.Id,
                tab.Name,
                tab.Pages
                    .OrderBy(page => page.Sequence)
                    .Select(page => new PageInfo(page.Id, page.Title, page.IsHot))
                    .ToList()
                )
            )
            .ToListAsync();

        // 上面这串Linq生成的Sql语句长这样：
        // SELECT[t].[Id], [t].[Name], [t0].[Id], [t0].[Title], [t0].[IsHot]
        // FROM[T_Tabs] AS[t]
        // LEFT JOIN(
        //     SELECT[t1].[Id], [t1].[Title], [t1].[IsHot], [t1].[Sequence], [t1].[TabId]
        //     FROM [T_Pages] AS [t1]
        // ) AS[t0] ON[t].[Id] = [t0].[TabId]
        // ORDER BY[t].[Sequence], [t].[Id], [t0].[Sequence]
    }

    public async Task<GetPageResponse?> GetPageAsync(Guid pageId)
    {
        var page = await _dbContext.Pages
            .AsNoTracking()
            .Include(page => page.QandAs)
            .Select(page => new
            {
                page.Id,
                Response = new GetPageResponse(page.IsPureQandA, page.Title, page.HtmlUrl, page.QandAs
                        .OrderBy(q => q.Sequence)
                        .Select(q => new QandAVM(q.Id, q.Question, q.Answer))
                        .ToList()
                    )
            })
            .FirstOrDefaultAsync(a => a.Id == pageId);
        if (page == null)
        {
            return null;
        }
        return page.Response;

        // 从生成的Sql语句可以看出：上面这个也是先根据Id找出Page再与T_QandAs进行Join查询，而不是说因为First写在后面就先Join查询再First。与下面的写法的区别仅在于上面这种写法只查询了需要的列，故选择上面这种写法。

        // SELECT[t0].[Id], [t0].[IsPureQandA], [t0].[HtmlUrl], [t1].[Question], [t1].[Answer], [t1].[Id]
        // FROM(
        //     SELECT TOP(1)[t].[Id], [t].[IsPureQandA], [t].[HtmlUrl]
        //     FROM[T_Pages] AS[t]
        //     WHERE[t].[Id] = @__pageId_0
        // ) AS[t0]
        // LEFT JOIN(
        // SELECT[t2].[Question], [t2].[Answer], [t2].[Id], [t2].[Sequence], [t2].[PageId]
        //     FROM [T_QandAs] AS [t2]
        // ) AS[t1] ON[t0].[Id] = [t1].[PageId]
        // ORDER BY[t0].[Id], [t1].[Sequence]


        //Page? page = await _dbContext.Pages
        //    .AsNoTracking()
        //    .Include(page => page.QandAs)
        //    .FirstOrDefaultAsync(page => page.Id == pageId);
        //if (page == null)
        //{
        //    return null;
        //}
        //return new GetPageResponse(page.IsPureQandA, page.HtmlUrl, page.QandAs.OrderBy(q => q.Sequence).Select(q => new QandAVM(q.Question, q.Answer)).ToList());

        // SELECT[t0].[Id], [t0].[HtmlUrl], [t0].[IsHot], [t0].[IsPureQandA], [t0].[Sequence], [t0].[TabId], [t0].[Title], [t1].[Id], [t1].[Answer], [t1].[PageId], [t1].[Question], [t1].[Sequence]
        // FROM(
        //     SELECT TOP(1)[t].[Id], [t].[HtmlUrl], [t].[IsHot], [t].[IsPureQandA], [t].[Sequence], [t].[TabId], [t].[Title]
        //     FROM[T_Pages] AS[t]
        //     WHERE[t].[Id] = @__pageId_0
        // ) AS[t0]
        // LEFT JOIN[T_QandAs] AS[t1] ON[t0].[Id] = [t1].[PageId]
        // ORDER BY[t0].[Id]
    }
}
