using SubmitService.Domain.Entities.Enums;

namespace SubmitService.Submit.WebAPI.Controllers.Responses;

public record SubmissionVMforSubmitter(string Status, List<ParagraphVM> Paragraphs);

public record ParagraphVM(int Sequence, DateTime CreationTime, string Sender, string TextContent, List<string> pictureUrls);
