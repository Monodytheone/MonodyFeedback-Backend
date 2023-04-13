using MediatR;
using Microsoft.AspNetCore.SignalR;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Notifications;
using SubmitService.Process.WebAPI.Hubs;

namespace SubmitService.Process.WebAPI.NotificationHandlers;

public class SubmissionToBeEvaluatedNotificationHandler
    : INotificationHandler<SubmissionToBeEvaluatedNotification>
{
    private readonly ISubmitRepository _repository;
    private readonly IHubContext<SubmitterHub> _submitterHubContext;

    public SubmissionToBeEvaluatedNotificationHandler(ISubmitRepository repository, IHubContext<SubmitterHub> submitterHubContext)
    {
        _repository = repository;
        _submitterHubContext = submitterHubContext;
    }

    public Task Handle(SubmissionToBeEvaluatedNotification notification, CancellationToken cancellationToken)
    {
        Submission submission = notification.Submission;
        string describe = _repository.GetDescribeOfSubmission(submission.Id);
        SubmissionInfo submissionInfo = new(submission.Id.ToString(), describe, submission.LastInteractionTime, submission.SubmissionStatus);
        return _submitterHubContext.Clients.User(submission.SubmitterId.ToString())
            .SendAsync("SubmissionToBeEvaluated", submissionInfo);
    }
}
