using CommonInfrastructure.Filters.Transaction;
using CommonInfrastructure.TencentCOS;
using CommonInfrastructure.TencentCOS.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Entities.Enums;
using SubmitService.Infrastructure;
using SubmitService.Process.WebAPI.Controllers.Requests;
using SubmitService.Process.WebAPI.Controllers.Responses;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TencentCloud.Scf.V20180416.Models;
using Zack.ASPNETCore;

namespace SubmitService.Process.WebAPI.Controllers;

[Route("api/[controller]/[action]")]
[Authorize(Roles = "processor")]
[ApiController]
public class ProcessorController : ControllerBase
{
    private readonly SubmitDomainService _domainService;
    private readonly ISubmitRepository _repository;
    private readonly SubmitDbContext _dbContext;
    private readonly COSService _cosService;
    private readonly IOptionsSnapshot<COSPictureOptions> _cosPictureOptions;

    // Validators of FluentValidation: 
    private readonly IValidator<ProcessRequest> _processValidator;

    public ProcessorController(SubmitDomainService domainService, ISubmitRepository submitRepository, SubmitDbContext dbContext, IOptionsSnapshot<COSPictureOptions> cosPictureOptions, COSService cosService, IValidator<ProcessRequest> processValidator)
    {
        _domainService = domainService;
        _repository = submitRepository;
        _dbContext = dbContext;
        _cosPictureOptions = cosPictureOptions;
        _cosService = cosService;
        _processValidator = processValidator;
    }

    /// <summary>
    /// 获取未分配的Submission的数量
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Roles = "processor,master")]
    public Task<int> GetUnassignedNumber()
    {
        return _dbContext.Submissions.CountAsync(submission => submission.SubmissionStatus ==
            Domain.Entities.Enums.SubmissionStatus.ToBeAssigned);
    }

    /// <summary>
    /// 分配5个Submission给processor
    /// <para>不启用工作单元，为了及时处理并发，本方法涉及到的仓储操作会立即UpdateDatabase</para>
    /// </summary>
    [HttpPost]
    [NotTransactional]
    public async Task<ActionResult<List<SubmissionInfo>>> AssignSubmissions()
    {
        string processorId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (await _repository.GetToBeProcessedNumberOfProcessorAsync(processorId) >= 10)
        {
            return BadRequest("请将你已领取的问题处理至少于10个后再来领取新的问题");
        }

        List<SubmissionInfo> assignedList = await _domainService.AssignSubmissionsAsync(processorId);
        return assignedList;
    }

    /// <summary>
    /// 获取处理者的全部未处理问题的简略信息
    /// <para>按照提交时间由早到晚排序</para>
    /// </summary>
    [HttpGet]
    public Task<List<SubmissionInfo>> GetToBeProcessedSubmissionInfosOfProcessor()
    {
        string processorId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return _repository.GetToBeProcessedSubmissionInfosOfProcessorAsync(processorId);
    }

    /// <summary>
    /// 获取处理者的某个状态的全部问题的简略信息
    /// <para>按照提交时间由晚到早排序</para>
    /// </summary>
    /// <param name="status">已关闭/待完善/待评价</param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<List<SubmissionInfo>>> GetSubmissionInfosInStatus(SubmissionStatus status)
    {
        if (status == SubmissionStatus.ToBeAssigned || status == SubmissionStatus.ToBeProcessed)
        {
            return BadRequest($"调错方法了：“{status}”状态的问题列表不可以用GetSubmissionInfosOfStatus来获取，因为排序方式不一样");
        }

        Guid processorId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        return await _repository.GetSubmissionInfosOfProcessorInStatus_InOrderFromLaterToEarly_Async(processorId, status);
    }

    /// <summary>
    /// 获取处理者拥有的某种状态的问题的数量
    /// </summary>
    [HttpGet("{status}")]
    public async Task<ActionResult<int>> GetNumberOfSubmissionsInStatus(SubmissionStatus status)
    {
        if (status == SubmissionStatus.ToBeAssigned)
        {
            return BadRequest("不可以试图获取处理者拥有的“待分配”问题数量");
        }

        Guid processorId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        return await _dbContext.Submissions
            .Where(submission => submission.ProcessorId == processorId)
            .Where(submission => submission.SubmissionStatus == status)
            .CountAsync();
    }

    /// <summary>
    /// 分页地获取处理者的某个状态的全部问题的简略信息
    /// </summary>
    /// <returns>按照最后交互时间从晚到早排序</returns>
    [HttpGet("{status}/page/{page}")]
    public async Task<ActionResult<List<SubmissionInfo>>> GetPaginatedSubmissionInfosInStatus(SubmissionStatus status, int page, int pageSize)
    {
        // Get请求不能有请求体，无法使用FluentValidation进行数据校验；倒是可以改成Put请求，但这严重不符合Http语义；
        // 故就这么一个if一个if地写了
        if (status == SubmissionStatus.ToBeAssigned)
        {
            return BadRequest("不可以试图查询处理者拥有的“待分配”问题");
        }
        if(status == SubmissionStatus.ToBeProcessed)
        {
            return BadRequest("调错方法了：“未处理”状态的问题列表不可以用GetPainatedSubmissionInfosInStatus来获取，因为排序方式不一样");
        }
        if (page < 1)
        {
            return BadRequest("页码不得小于1");
        }
        if (pageSize < 3)
        {
            return BadRequest("一页至少3个");
        }

        Guid processorId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        return await _repository.PaginatlyGetSubmissionInfosOfProcessor_InStatus_InOrderFromLaterToEarly_Async(processorId, status, page, pageSize);
    }

    /// <summary>
    /// 处理者获取其管理的一个问题的详细信息
    /// </summary>
    [HttpGet("{submissionId}")]
    public async Task<ActionResult<SubmissionVMforProcessor>> GetSubmission(string submissionId)
    {
        Submission? submission = await _dbContext.Submissions
            .Include(submission => submission.Paragraphs)
            .FirstOrDefaultAsync(submission => submission.Id.ToString() == submissionId);
        if(submission == null)
        {
            return NotFound();
        }

        if (submission.ProcessorId.ToString() != this.User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Forbid();
        }

        List<ParagraphVM> paragraphVMs = new();
        foreach (Paragraph paragraph in submission.Paragraphs)
        {
            List<string> pictureUrls = await _repository.GetPictureUrlsOfParagraphAsync(submissionId,
                paragraph.SequenceInSubmission, 60);
            paragraphVMs.Add(new(paragraph.SequenceInSubmission, paragraph.CreationTime,
                paragraph.Sender.ToString(), paragraph.TextContent, pictureUrls));
            paragraphVMs = paragraphVMs.OrderBy(paragraphVM => paragraphVM.Sequence).ToList();
        }
        return new SubmissionVMforProcessor(submission.SubmitterTelNumber, submission.SubmitterEmail,
            submission.SubmitterId.ToString(), submission.SubmitterName, submission.SubmissionStatus, 
            paragraphVMs);
    }

    /// <summary>
    /// 获取处理者向PictureFolder中的任何文件夹上传图片的临时密钥
    /// </summary>
    [HttpGet]
    public ActionResult<TempCredentialResponse> AskPutPictureTempCredential()
    {
        COSPictureOptions cosOptions = _cosPictureOptions.Value;
        string allowPrefix = $"{cosOptions.PictureFolder}/*";
        return _cosService.GeneratePutObjectTempCredential(cosOptions.Bucket, cosOptions.Region, cosOptions.SecretId, cosOptions.SecretKey, allowPrefix, 120);
    }

    [HttpPost]
    [UnitOfWork(typeof(SubmitDbContext))]
    public async Task<ActionResult> Process(ProcessRequest request)
    {
        var validationResult = await _processValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        // 下面这两条其实可以写进校验器里作为异步校验规则，但我认为那不是个好选择
        // 不存在则404：
        Submission? submission = await _dbContext.Submissions
            .Include(submission => submission.Paragraphs)
            .FirstOrDefaultAsync(submission => submission.Id == request.SubmissionId);
        if (submission == null)
        {
            return NotFound("问题不存在");
        }
        // 问题不属于当前登录的处理者则403：
        Guid processorId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (submission.ProcessorId != processorId)
        {
            return Forbid();
        }


        List<Picture> pictures = new();
        byte pictureSequence = 1;
        foreach (PictureInfo pictureInfo in request.PictureInfos)
        {
            pictures.Add(new(pictureInfo.BucketName, pictureInfo.Region, pictureInfo.FullObjectKey,
                pictureSequence++));
        }
        bool processResult = _domainService.Process(submission, request.NextStatus, request.TextContent, pictures);
        if (processResult == false)
        {
            return BadRequest("该问题处于不可处理的状态");
        }
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<GetEvaluationResponse>> GetEvaluation([RequiredGuid] Guid submissionId)
    {
        var evaluation = await _dbContext.Submissions
            .AsNoTracking()  // 性能优化：不进行不必要的跟踪
            .Select(submission => new { submission.Id, submission.ProcessorId, submission.SubmissionStatus, submission.Evaluation })  // 性能优化：只获取需要的列
            .FirstOrDefaultAsync(a => a.Id == submissionId);
        if (evaluation == null)
        {
            return NotFound("问题不存在");
        }

        // 问题不属于当前登录的提交者则403
        Guid processorId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (evaluation.ProcessorId != processorId)
        {
            return Forbid();
        }

        if (evaluation.SubmissionStatus != SubmissionStatus.Closed)
        {
            return BadRequest("当前问题处于不可能拥有评价的状态");
        }

        if (evaluation.Evaluation == null)
        {
            return new GetEvaluationResponse(null, null);
        }
        else
        {
            return new GetEvaluationResponse(evaluation.Evaluation.IsSolved, evaluation.Evaluation.Grade);
        }
    }
}
