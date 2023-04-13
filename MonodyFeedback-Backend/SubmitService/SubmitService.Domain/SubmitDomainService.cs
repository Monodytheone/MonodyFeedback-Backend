using SubmitService.Domain.Entities;
using SubmitService.Domain.Entities.Enums;
using SubmitService.Domain.Notifications;
using System.Reflection.Metadata;

namespace SubmitService.Domain;

public class SubmitDomainService
{
    private readonly ISubmitRepository _submitRepository;

    public SubmitDomainService(ISubmitRepository submitRepository)
    {
        _submitRepository = submitRepository;
    }

    public Submission CreateSubmissionWithFirstParagraph(Guid submitterId, string submitterName, string? telNumber, string? email, string textContent, List<Picture> pictures)
    {
        return Submission.Create(submitterId, submitterName, telNumber, email, textContent, pictures);
    }

    /// <summary>
    /// 给处理者分配5个问题
    /// </summary>
    /// <param name="processorId"></param>
    /// <returns></returns>
    public async Task<List<SubmissionInfo>> AssignSubmissionsAsync(string processorId, int assignNumber = 5)
    {
        // 1. 获取提交时间最早的5个未分配Submission，返回它们的SubmissionInfo
        // 2. 更新这5个Submission的处理者Id和状态
        // ps: 已将processorId设为并发令牌，并已启用事务管理，若冲突了，会自动回滚的
        List<SubmissionInfo> unassignedSubmissionInfos =
            await _submitRepository.GetUnassignedSubmissionInfosAsync(assignNumber);
        if (unassignedSubmissionInfos.Count == 0)
        {
            return unassignedSubmissionInfos;
        }

        var assignResult =
            await _submitRepository.AssignAsync(Guid.Parse(processorId), unassignedSubmissionInfos);
        return assignResult.successedList;

        // 如果分配都成功了，未出现被"抢走"的情况，直接返回
        //if (assignResult.failureNumber == 0)
        //{
        //return assignResult.successedList;
        //}
        //else  // 若有分配失败的，失败几个就再分配几个
        //{
        //    assignResult.successedList.AddRange(await AssignSubmissionsAsync(processorId, assignResult.failureNumber));
        //    return assignResult.successedList;
        //}
    }

    /// <summary>
    /// "处理"问题
    /// <para>只能对待处理问题使用，能将状态变为"待评价"或"待完善"</para>
    /// </summary>
    public bool Process(Submission submission, SubmissionStatus nextStatus, string textContent, List<Picture> pictures)
    {
        if (submission.SubmissionStatus != SubmissionStatus.ToBeProcessed)
        {
            return false;
        }

        submission.AddParagraph(textContent, Sender.Processor, pictures).ChangeStatus(nextStatus);
        return true;
    }

    /// <summary>
    /// "完善"问题
    /// <para>只能对待完善问题使用，将状态变为"待处理"</para>
    /// </summary>
    public bool Supplement(Submission submission, string textContent, List<Picture> pictures)
    {
        if (submission.SubmissionStatus != SubmissionStatus.ToBeSupplemented)
        {
            return false;
        }

        submission.AddParagraph(textContent, Sender.Submitter, pictures)
            .ChangeStatus(SubmissionStatus.ToBeProcessed)
            .AddDomainEventIfAbsent(new SubmissionToBeProcessedNotification(submission));
        return true;
    }

    public bool Evaluate(Submission submission, bool isSolved, byte grade)
    {
        if (submission.SubmissionStatus != SubmissionStatus.ToBeEvaluated)
        {
            return false;
        }

        submission.SetEvaluation(isSolved, grade);
        return true;
    }

    // 虽只是对仓储服务的转发，但Submission的自动关闭显然属于核心业务逻辑，故写进领域服务
    public async Task CloseSubmissionsWaitLong_InToBeEvaluatedStatus_Async(int waitingSeconds = 172800)
    {
        TimeSpan waitingTimeSpan = TimeSpan.FromSeconds(waitingSeconds);
        await _submitRepository.CloseSubmissionsUnevaluatedForLongAsync(waitingTimeSpan);
    }

    public async Task CloseSubmissionsWaitLong_InToBeSupplementStatus_Async(int waitingSeconds = 172800)
    {
        TimeSpan waitingTimeSpan = TimeSpan.FromSeconds(waitingSeconds);
        await _submitRepository.CloseSubmissionsUnsupplementedForLongAsync(waitingTimeSpan);
    }
}
