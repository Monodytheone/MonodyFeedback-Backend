using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

/// <summary>
/// Submission被提交者（通过评价）关闭
/// </summary>
/// <param name="Submission"></param>
public record SubmissionCloseViaEvaluateNotification(Submission Submission) : INotification;
