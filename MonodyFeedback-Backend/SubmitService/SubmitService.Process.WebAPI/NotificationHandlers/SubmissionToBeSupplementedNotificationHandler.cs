using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Entities.Enums;
using SubmitService.Domain.Notifications;
using SubmitService.Infrastructure;
using SubmitService.Process.WebAPI.Hubs;

namespace SubmitService.Process.WebAPI.NotificationHandlers;

public class SubmissionToBeSupplementedNotificationHandler
    : INotificationHandler<SubmissionToBeSupplementedNotification>
{
    private readonly IHubContext<SubmitterHub> _submitterHubContext;
    private readonly ISubmitRepository _repository;

    public SubmissionToBeSupplementedNotificationHandler(IHubContext<SubmitterHub> submitterHubContext, ISubmitRepository repository)
    {
        _submitterHubContext = submitterHubContext;
        _repository = repository;
    }

    public async Task Handle(SubmissionToBeSupplementedNotification notification, CancellationToken cancellationToken)
    {
        Submission submission = notification.Submission;
        string describe = _repository.GetDescribeOfSubmission(submission.Id);
        SubmissionInfo submissionInfo = new(submission.Id.ToString(), describe, submission.LastInteractionTime, submission.SubmissionStatus);
        await _submitterHubContext.Clients.User(submission.SubmitterId.ToString()).SendAsync("SubmissionToBeSupplemented", submissionInfo);  // 通知客户端有新的待完善问题
    }
}
