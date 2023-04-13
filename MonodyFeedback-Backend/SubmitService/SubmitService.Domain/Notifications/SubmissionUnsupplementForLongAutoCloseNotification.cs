using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

/// <summary>
/// Submission因长时间未完善而自动关闭的领域事件
/// </summary>
public record SubmissionUnsupplementForLongAutoCloseNotification(Submission Submission) : INotification;
