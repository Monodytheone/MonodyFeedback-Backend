namespace FAQService.WebAPI.Controllers.Responses;

public record GetPageResponse(bool IsPureQandA, string? HtmlUrl, List<QandAVM>? QandAs);

public record QandAVM(string Question, string Answer);
