using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

/// <summary>
/// Submission进入待处理状态
/// </summary>
/// <param name="Submission"></param>
public record SubmissionToBeProcessedNotification(Submission Submission) : INotification;
