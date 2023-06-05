namespace FAQService.WebAPI.Controllers.Responses;

public record GetPageResponse(bool IsPureQandA, string Title, string? HtmlUrl, List<QandAVM>? QandAs);

public record QandAVM(Guid QandAId, string Question, string Answer);
