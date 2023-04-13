using MediatR;
using Microsoft.AspNetCore.SignalR;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Notifications;
using SubmitService.Submit.WebAPI.Hubs;

namespace SubmitService.Submit.WebAPI.NotificationHandlers;

public class SubmissionCloseViaEvaluateNotificationHandler
    : INotificationHandler<SubmissionCloseViaEvaluateNotification>
{
    private readonly ISubmitRepository _repository;
    private readonly IHubContext<ProcessorHub> _processorHubContext;


    public SubmissionCloseViaEvaluateNotificationHandler(ISubmitRepository repository, IHubContext<ProcessorHub> processorHubContext)
    {
        _repository = repository;
        _processorHubContext = processorHubContext;
    }


    public Task Handle(SubmissionCloseViaEvaluateNotification notification, CancellationToken cancellationToken)
    {
        Submission submission = notification.Submission;
        string describe = _repository.GetDescribeOfSubmission(submission.Id);
        SubmissionInfo info = new(submission.Id.ToString(), describe, submission.LastInteractionTime, submission.SubmissionStatus);
        return _processorHubContext.Clients.User(submission.ProcessorId.ToString()!)
            .SendAsync("SubmissionCloseByEvaluate", info);
    }
}