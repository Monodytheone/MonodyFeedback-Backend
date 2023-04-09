using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

/// <summary>
/// Submission进入已关闭状态
/// </summary>
/// <param name="Submission"></param>
public record SubmissionCloseNotification(Submission Submission) : INotification;
