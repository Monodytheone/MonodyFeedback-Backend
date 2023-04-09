using SubmitService.Domain.Entities;
using SubmitService.Domain.Entities.Enums;

namespace SubmitService.Domain;

public interface ISubmitRepository
{
    Task<List<string>> GetPictureUrlsOfParagraphAsync(string submissionId, int paragraphSequence, long durationSeconds);

    Task<List<SubmissionInfo>> GetSubmissionInfosOfSubmitterAsync(string submitterId);
    
    Task<List<SubmissionInfo>> GetToBeProcessedSubmissionInfosOfProcessorAsync(string processorId);

    /// <summary>
    /// 获取某个处理者拥有的某个状态的全部Submission，的简略信息
    /// <para>按照最后交互时间，从晚到早排序</para>
    /// <para>为了准确表意，决定如此命名，如有其他观点，请向我提出</para>
    /// </summary>
    Task<List<SubmissionInfo>> GetSubmissionInfosOfProcessorInStatus_InOrderFromLaterToEarly_Async(Guid processorId, SubmissionStatus status);

    Task<int> GetToBeProcessedNumberOfProcessorAsync(string processorId);

    /// <summary>
    /// 获取未分配的Submission的简略信息
    /// </summary>
    /// <param name="number">获取的数量，默认5个</param>
    /// <returns></returns>
    Task<List<SubmissionInfo>> GetUnassignedSubmissionInfosAsync(int number);

    Task<(List<SubmissionInfo> successedList, int failureNumber)> AssignAsync(Guid processorId, List<SubmissionInfo> submssionInfos);

    /// <summary>
    /// 关闭长时间未评价的Submission
    /// </summary>
    public Task CloseSubmissionsUnevaluatedForLongAsync(TimeSpan waitingTime);

    /// <summary>
    /// 关闭长时间未完善的Submission
    /// </summary>
    public Task CloseSubmissionsUnsupplementedForLongAsync(TimeSpan waitingTime);
}
