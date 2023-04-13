using MediatR;
using Microsoft.AspNetCore.SignalR;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Notifications;
using SubmitService.Process.WebAPI.Hubs;

namespace SubmitService.Process.WebAPI.NotificationHandlers;

public class SubmissionUnsupplementForLongAutoCloseNotificationHandler
    : INotificationHandler<SubmissionUnsupplementForLongAutoCloseNotification>
{
    private readonly ISubmitRepository _repository;
    private readonly IHubContext<CommonHub> _commonHubContext;

    public SubmissionUnsupplementForLongAutoCloseNotificationHandler(ISubmitRepository repository, IHubContext<CommonHub> commonHubContext)
    {
        _repository = repository;
        _commonHubContext = commonHubContext;
    }
    public Task Handle(SubmissionUnsupplementForLongAutoCloseNotification notification, CancellationToken cancellationToken)
    {
        Submission submission = notification.Submission;
        string describe = _repository.GetDescribeOfSubmission(submission.Id);
        SubmissionInfo info = new(submission.Id.ToString(), describe, submission.LastInteractionTime, submission.SubmissionStatus);
        return _commonHubContext.Clients
            .Users(submission.SubmitterId.ToString(), submission.ProcessorId.ToString()!)
            .SendAsync("SubmissionUnsupplementForLongAutoClose", info);
    }
}
