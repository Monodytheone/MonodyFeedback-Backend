namespace FAQService.WebAPI.Controllers.Responses;

public record TabVM(Guid TabId, string TabName, List<PageInfo> PageInfos);

public record PageInfo(Guid PageId, string Title, bool IsHot);
