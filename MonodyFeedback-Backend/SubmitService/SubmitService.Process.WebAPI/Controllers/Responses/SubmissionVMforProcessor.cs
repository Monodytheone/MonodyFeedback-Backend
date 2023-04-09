using SubmitService.Domain.Entities.Enums;

namespace SubmitService.Process.WebAPI.Controllers.Responses;

public record SubmissionVMforProcessor(string? SubmitterTel, string? SubmitterEmail, string SubmitterId, string SubmitterName, 
    SubmissionStatus Status, List<ParagraphVM> Paragraphs);

public record ParagraphVM(int Sequence, DateTime CreationTime, string Sender, string TextContent, List<string> pictureUrls);
