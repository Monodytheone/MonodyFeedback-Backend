using CommonInfrastructure.TencentCOS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Entities.Enums;

namespace SubmitService.Infrastructure;

public class SubmitRepository : ISubmitRepository
{
    private readonly SubmitDbContext _dbContext;
    private readonly COSService _cosService;
    private readonly IOptionsSnapshot<COSPictureOptions> _cosPictureOptions;

    public SubmitRepository(SubmitDbContext dbContext, COSService cosService, IOptionsSnapshot<COSPictureOptions> cosPictureOptions)
    {
        _dbContext = dbContext;
        _cosService = cosService;
        _cosPictureOptions = cosPictureOptions;
    }

    public async Task<List<string>> GetPictureUrlsOfParagraphAsync(string submissionId, int paragraphSequence, long durationSeconds)
    {
        Paragraph paragraph = (await _dbContext.Submissions.AsNoTracking()  // 性能优化：不进行不必要的跟踪
            .Include(submission => submission.Paragraphs).ThenInclude(paragraph => paragraph.Pictures)
            .FirstAsync(submission => submission.Id.ToString() == submissionId))
            .Paragraphs.Single(paragraph => paragraph.SequenceInSubmission == paragraphSequence);
        string[] objectKeys = paragraph.Pictures.Select(picture => picture.FullObjectKey).ToArray();

        COSPictureOptions pictureOptions = _cosPictureOptions.Value;
        return _cosService.GeneratePreSignatureAvatarUrls(pictureOptions.AppId, pictureOptions.Region, pictureOptions.SecretId, pictureOptions.SecretKey, durationSeconds, pictureOptions.Bucket, objectKeys);
    }

    public async Task<List<SubmissionInfo>> GetSubmissionInfosOfSubmitterAsync(string submitterId)
    {
        Guid submitterGuid = Guid.Parse(submitterId);

        IQueryable<SubmissionInfo> submissionInfos = _dbContext.Submissions
            .AsNoTracking()  // 性能优化：不进行不必要的跟踪
            .Include(submission => submission.Paragraphs)
            .Where(submission => submission.SubmitterId == submitterGuid)
            .Select(submission => new SubmissionInfo(  // 性能优化：只获取需要的列
                submission.Id.ToString(),
                submission.Paragraphs[0].TextContent.GetFirst15Wrods(),  // 第一个Paragraph的前15个字符
                submission.LastInteractionTime,
                submission.SubmissionStatus
            ));  // 不能从这里继续OrderBy了，无法生成Sql语句

        List<SubmissionInfo> infoList = await submissionInfos.ToListAsync();
        return infoList.OrderByDescending(info => info.LastInteractionTime).ToList();
    }

    public async Task<List<SubmissionInfo>> GetToBeProcessedSubmissionInfosOfProcessorAsync(string processorId)
    {
        Guid processorGuid = Guid.Parse(processorId);
        IQueryable<SubmissionInfo> submissionInfos = _dbContext.Submissions
            .AsNoTracking()
            .Include(submission => submission.Paragraphs)
            .Where(submission => submission.ProcessorId == processorGuid
                && submission.SubmissionStatus == SubmissionStatus.ToBeProcessed)
            .Select(submission => new SubmissionInfo(
                submission.Id.ToString(),
                submission.Paragraphs[0].TextContent.GetFirst15Wrods(),  // 第一个Paragraph的前15个字符
                submission.LastInteractionTime,
                submission.SubmissionStatus
            ));

        List<SubmissionInfo> infoList = await submissionInfos.ToListAsync();
        // 与提交者的问题列表不同，处理者的未处理问题列表中提交时间越早排得越靠前
        return infoList.OrderBy(info => info.LastInteractionTime).ToList();  
    }

    public async Task<List<SubmissionInfo>> GetSubmissionInfosOfProcessorInStatus_InOrderFromLaterToEarly_Async(Guid processorId, SubmissionStatus status)
    {
        IQueryable<SubmissionInfo> submissionInfos = _dbContext.Submissions
            .AsNoTracking()
            .Include(submission => submission.Paragraphs)
            .Where(submission => submission.ProcessorId == processorId && submission.SubmissionStatus == status)
            .Select(submission => new SubmissionInfo(
                submission.Id.ToString(),
                submission.Paragraphs.First(paragraph => paragraph.SequenceInSubmission == 1)
                    .TextContent.GetFirst15Wrods(),
                submission.LastInteractionTime,
                submission.SubmissionStatus
            ));
        return (await submissionInfos.ToListAsync())
            .OrderByDescending(info => info.LastInteractionTime).ToList();
    }


    public Task<List<SubmissionInfo>> PaginatlyGetSubmissionInfosOfProcessor_InStatus_InOrderFromLaterToEarly_Async(Guid processorId, SubmissionStatus status, int page, int pageSize = 10)
    {
        int numOfSkip = (page - 1) * pageSize;
        return _dbContext.Submissions
            .AsNoTracking()
            .Include(submission => submission.Paragraphs)
            .Where(submission => submission.ProcessorId == processorId && submission.SubmissionStatus == status)
            .OrderByDescending(submission => submission.LastInteractionTime)
            .Skip(numOfSkip).Take(pageSize)
            .Select(submission => new SubmissionInfo(
                submission.Id.ToString(),
                submission.Paragraphs.First(paragraph => paragraph.SequenceInSubmission == 1).TextContent.GetFirst15Wrods(),
                submission.LastInteractionTime,
                submission.SubmissionStatus
            ))
            .ToListAsync();
    }

    public Task<int> GetToBeProcessedNumberOfProcessorAsync(string processorId)
    {
        return _dbContext.Submissions
            .Where(submission => submission.ProcessorId == Guid.Parse(processorId))
            .CountAsync(submission => submission.SubmissionStatus == SubmissionStatus.ToBeProcessed);
    }

    public Task<List<SubmissionInfo>> GetUnassignedSubmissionInfosAsync(int number)
    {
        return _dbContext.Submissions
           .Where(submission => submission.SubmissionStatus == SubmissionStatus.ToBeAssigned)
           .OrderBy(submission => submission.CreationTime)
           .Take(number)
           .Select(submission => new SubmissionInfo(
               submission.Id.ToString(),
               submission.Paragraphs[0].TextContent.GetFirst15Wrods(),
               submission.LastInteractionTime,
               submission.SubmissionStatus))
           .ToListAsync();
    }

    public async Task<(List<SubmissionInfo> successedList, int failureNumber)> AssignAsync(Guid processorId, List<SubmissionInfo> submissionInfos)
    {
        int failureNumber = 0;
        List<SubmissionInfo> successedList = new();

        foreach (SubmissionInfo submissionInfo in submissionInfos)
        {
            Submission submission = await _dbContext.Submissions
                .FirstAsync(submission => submission.Id == Guid.Parse(submissionInfo.Id));
            try
            {
                bool assignResult = submission.Assign(processorId);
                if (assignResult == true)
                {
                    submission.ChangeStatus(SubmissionStatus.ToBeProcessed);
                    await _dbContext.SaveChangesAsync();  // 立即更新数据到数据库，触发并发检查
                    submissionInfo.LastInteractionTime = DateTime.Now;  // 与数据库里存的不一样也无妨，前端只是用这个时间排一下序而已
                    submissionInfo.Status = SubmissionStatus.ToBeProcessed;
                    successedList.Add(submissionInfo);
                }
            }
            // 并发控制：处理被"抢走"的情况
            catch (DbUpdateConcurrencyException)
            {
                failureNumber++;
            }
        }

        return (successedList, failureNumber);
    }

    public async Task CloseSubmissionsUnevaluatedForLongAsync(TimeSpan waitingTime)
    {
        DateTime limitTime = (DateTime.Now - waitingTime);
        IQueryable<Submission> unevaluatedSubmissions = _dbContext.Submissions
            .Include(submission => submission.Paragraphs)  // 创建Paragraph时需要用到paragraph的数量
            // 下面两个都无法被翻译为Sql语句：
            //.Where(submission => DateTime.Now - submission.LastInteractionTime > waitingTime)
            //.Where(submission => submission.LastInteractionTime < DateTime.Now - waitingTime)
            .Where(submission => submission.LastInteractionTime < limitTime)
            .Where(submission => submission.SubmissionStatus == SubmissionStatus.ToBeEvaluated);
        await unevaluatedSubmissions.ForEachAsync(submission =>
        {
            submission.AddParagraph("因长时间未评价，问题已自动关闭", Sender.System, new()).Close();
        });
        await _dbContext.SaveChangesAsync();
    }

    public async Task CloseSubmissionsUnsupplementedForLongAsync(TimeSpan waitingTime)
    {
        DateTime limitTime = DateTime.Now - waitingTime;
        IQueryable<Submission> unsupplementSubmissions = _dbContext.Submissions
            .Include(submission => submission.Paragraphs)
            .Where(submission => submission.LastInteractionTime < limitTime)
            .Where(submission => submission.SubmissionStatus == SubmissionStatus.ToBeSupplemented);
        await unsupplementSubmissions.ForEachAsync(submission =>
        {
            submission.AddParagraph("因长时间未完善，问题已自动关闭", Sender.System, new()).Close();
        });
        await _dbContext.SaveChangesAsync();
    }
}
