using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

/// <summary>
/// Submission进入待完善状态
/// </summary>
/// <param name="Submission"></param>
public record SubmissionToBeSupplementedNotification(Submission Submission) : INotification;
