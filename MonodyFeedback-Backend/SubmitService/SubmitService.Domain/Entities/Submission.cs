using SubmitService.Domain.Entities.Enums;
using SubmitService.Domain.Entities.ValueObjects;
using SubmitService.Domain.Notifications;
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
            // 若电话和邮箱传进来的是空串，则赋null
            SubmitterTelNumber = submitterTelNumber == string.Empty ? null : submitterTelNumber,
            SubmitterEmail = submitterEmail == string.Empty ? null : submitterEmail,
            CreationTime = DateTime.Now,
            LastInteractionTime = DateTime.Now,
        };
        submission.Paragraphs.Add(new(submission, Sender.Submitter, textContent, pictures));
        submission.AddDomainEventIfAbsent(new SubmissionCreateNotification(submission));
        return submission;
    }

    /// <summary>
    /// 分配（指定处理者）
    /// </summary>
    public bool Assign(Guid processorId)
    {
        // 只是判断一下是否误将已分配的Submission进行分配了，此处并不能实现并发控制
        if (SubmissionStatus == SubmissionStatus.ToBeAssigned)
        {
            this.ProcessorId = processorId;
            this.LastInteractionTime = DateTime.Now;
            return true;
        }
        else
        {
            return false;
        }
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
        this.UpdateLastInteractionTime();

        // 发布领域事件。即使是可能用不到的领域事件如待处理Notification也尽量发布一下
        switch (status)
        {
            case SubmissionStatus.ToBeEvaluated:
                this.AddDomainEventIfAbsent(new SubmissionToBeEvaluatedNotification(this)); 
                break;
            case SubmissionStatus.ToBeSupplemented:
                this.AddDomainEventIfAbsent(new SubmissionToBeSupplementedNotification(this));
                break;
            case SubmissionStatus.Closed:
                this.AddDomainEventIfAbsent(new SubmissionCloseNotification(this));
                break;
            case SubmissionStatus.ToBeProcessed:
                this.AddDomainEventIfAbsent(new SubmissionToBeProcessedNotification(this));
                break;
        }
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
        this.ChangeStatus(SubmissionStatus.Closed);
        return this;
    }

    public Submission Close()
    {
        this.ClosingTime = DateTime.Now;
        this.ChangeStatus(SubmissionStatus.Closed);
        return this;
    }

}
