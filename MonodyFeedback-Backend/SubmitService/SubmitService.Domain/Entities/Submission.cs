using SubmitService.Domain.Entities.Enums;
using SubmitService.Domain.Entities.ValueObjects;
using Zack.DomainCommons.Models;

namespace SubmitService.Domain.Entities;

public record Submission : BaseEntity, IAggregateRoot
{
    public Guid SubmitterId { get; init; }

    public string SubmitterName { get; init; }

    /// <summary>
    /// 处理者Id（需要进行并发控制）
    /// </summary>
    public Guid? ProcessorId { get; private set; }

    /// <summary>
    /// 状态（待分配、待处理、待完善、待评价、已关闭）
    /// </summary>
    public SubmissionStatus SubmissionStatus { get; private set; }

    public string? SubmitterTelNumber { get; init; }

    public string? SubmitterEmail { get; init; }

    public List<Paragraph> Paragraphs { get; private set; } = new();

    /// <summary>
    /// 用户评价
    /// </summary>
    public Evaluation? Evaluation { get; private set; }

    public DateTime CreationTime { get; init; }

    /// <summary>
    /// 最后交互时间，用于自动关闭和排序
    /// </summary>
    public DateTime LastInteractionTime { get; private set; }

    public DateTime? ClosingTime { get; private set; }


    private Submission() { }

    /// <summary>
    /// 创建带有第一个Paragraph的Submission（因为用了record所以不方便写构造方法）
    /// </summary>
    public static Submission Create(Guid submitterId, string submitterName, string? submitterTelNumber, string? submitterEmail, string textContent, List<Picture> pictures)
    {
        Submission submission = new()  // 私有构造
        {
            Id = Guid.NewGuid(),
            SubmitterId = submitterId,
            SubmitterName = submitterName,
            SubmissionStatus = SubmissionStatus.ToBeAssigned,
            SubmitterTelNumber = submitterTelNumber,
            SubmitterEmail = submitterEmail,
            CreationTime = DateTime.Now,
            LastInteractionTime = DateTime.Now,
        };
        submission.Paragraphs.Add(new(submission, Sender.Submitter, textContent, pictures));
        return submission;
    }

    /// <summary>
    /// 分配（指定处理者）
    /// </summary>
    public Submission Assign(Guid processorId)
    {
        this.ProcessorId = processorId;
        return this;
    }

    public Submission AddParagraph(string textContent, Sender sender, List<Picture> pictures)
    {
        Paragraph newParagraph = new(this, sender, textContent, pictures);
        this.Paragraphs.Add(newParagraph);
        return this;
    }

    public Submission ChangeStatus(SubmissionStatus status)
    {
        this.SubmissionStatus = status;
        return this;
    }

    public Submission UpdateLastInteractionTime()
    {
        this.LastInteractionTime = DateTime.Now;
        return this;
    }

    public Submission SetEvaluation(bool isSolved, byte Grade)
    {
        this.Evaluation = new(isSolved, Grade);
        return this;
    }

    public Submission Close()
    {
        this.SubmissionStatus = SubmissionStatus.Closed; 
        this.ClosingTime = DateTime.Now;
        return this;
    }

}
