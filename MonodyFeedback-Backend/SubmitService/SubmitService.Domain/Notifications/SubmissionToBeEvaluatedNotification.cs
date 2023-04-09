using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

/// <summary>
/// Submission进入待评价状态
/// </summary>
public record SubmissionToBeEvaluatedNotification(Submission Submission) : INotification;
