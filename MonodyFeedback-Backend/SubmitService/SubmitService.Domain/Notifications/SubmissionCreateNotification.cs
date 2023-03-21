using MediatR;
using SubmitService.Domain.Entities;

namespace SubmitService.Domain.Notifications;

public record SubmissionCreateNotification(Submission Submission) : INotification;
