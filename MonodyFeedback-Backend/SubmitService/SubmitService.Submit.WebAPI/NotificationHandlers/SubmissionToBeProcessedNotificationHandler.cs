using MediatR;
using Microsoft.AspNetCore.SignalR;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Notifications;
using SubmitService.Submit.WebAPI.Hubs;

namespace SubmitService.Submit.WebAPI.NotificationHandlers;

/// <summary>
/// 能收到这个事件只有“提交者完善了一个问题”这一种情况
/// </summary>
public class SubmissionToBeProcessedNotificationHandler
    : INotificationHandler<SubmissionToBeProcessedNotification>
{
    private readonly ISubmitRepository _repository;
    private readonly IHubContext<ProcessorHub> _processorHubContext;

    public SubmissionToBeProcessedNotificationHandler(ISubmitRepository repository, IHubContext<ProcessorHub> processorHubContext)
    {
        _repository = repository;
        _processorHubContext = processorHubContext;
    }

    public Task Handle(SubmissionToBeProcessedNotification notification, CancellationToken cancellationToken)
    {
        Submission submission = notification.Submission;
        string describe = _repository.GetDescribeOfSubmission(submission.Id);
        SubmissionInfo info = new(submission.Id.ToString(), describe, submission.LastInteractionTime, submission.SubmissionStatus);
        return _processorHubContext.Clients.User(submission.ProcessorId.ToString()!).SendAsync("SubmissionToBeProcessed", info);
    }
}
