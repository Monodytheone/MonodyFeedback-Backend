using SubmitService.Domain.Entities.Enums;

namespace SubmitService.Domain;


public class SubmissionInfo
{
    public string Id { get; init; }
    public string Describe { get; init; }
    public DateTime LastInteractionTime { get; set; }
    public SubmissionStatus Status { get; set; }

    /// <param name="describe">第一个Paragraph的前15个字</param>
    /// <param name="lastInteractionTime">最后交互时间</param>
    /// <param name="status"></param>
    public SubmissionInfo(string id, string describe, DateTime lastInteractionTime, SubmissionStatus status)
    {
        Id = id;
        Describe = describe;
        LastInteractionTime = lastInteractionTime;
        Status = status;
    }
}