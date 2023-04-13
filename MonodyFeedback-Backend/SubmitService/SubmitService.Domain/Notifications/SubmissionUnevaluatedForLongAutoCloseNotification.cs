using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

/// <summary>
/// 问题因长时间未评价而自动关闭的领域事件
/// </summary>
/// <param name="Submission"></param>
public record SubmissionUnevaluatedForLongAutoCloseNotification(Submission Submission) : INotification;
