using CommonInfrastructure.TencentCOS;
using CommonInfrastructure.TencentCOS.Responses;
using COSXML.Network;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Domain.Entities.Enums;
using SubmitService.Infrastructure;
using SubmitService.Submit.WebAPI.Controllers.Requests;
using SubmitService.Submit.WebAPI.Controllers.Responses;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Zack.ASPNETCore;

namespace SubmitService.Submit.WebAPI.Controllers;

[Route("api/[controller]/[action]")]
[Authorize(Roles = "submitter")]
[UnitOfWork(typeof(SubmitDbContext))]
[ApiController]
public class SubmitterController : ControllerBase
{
    private readonly SubmitDomainService _domainService;
    private readonly ISubmitRepository _repository;
    private readonly SubmitDbContext _dbContext;
    private readonly IOptionsSnapshot<COSPictureOptions> _pictureOptions;
    private readonly COSService _cosService;

    // Validators of FluentValidation:
    private readonly IValidator<SubmitRequest> _submitValidator;
    private readonly IValidator<SupplementRequest> _supplementValidator;
    private readonly IValidator<EvaluateRequest> _evaluateValidator;

    public SubmitterController(SubmitDomainService submitDomainService, ISubmitRepository repository, IValidator<SubmitRequest> submitValidator, SubmitDbContext dbContext, IOptionsSnapshot<COSPictureOptions> pictureOptions, COSService cosService, IValidator<SupplementRequest> supplementValidator, IValidator<EvaluateRequest> evaluateValidator)
    {
        _domainService = submitDomainService;
        _repository = repository;
        _submitValidator = submitValidator;
        _dbContext = dbContext;
        _pictureOptions = pictureOptions;
        _cosService = cosService;
        _supplementValidator = supplementValidator;
        _evaluateValidator = evaluateValidator;
    }

    [HttpGet]
    public ActionResult<TempCredentialResponse> AskPutPictureTempCredentialOfSubmitter()
    {
        string userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        COSPictureOptions cosOptions = _pictureOptions.Value;
        string allowPrefix = $"{cosOptions.PictureFolder}/{userId}/*";
        return _cosService.GeneratePutObjectTempCredential(cosOptions.Bucket, cosOptions.Region, cosOptions.SecretId, cosOptions.SecretKey, allowPrefix, 120);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Submit(SubmitRequest request)
    {
        var validationResult = await _submitValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        Guid submitterId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        string submitterName = this.User.FindFirstValue(ClaimTypes.Name);
        List<Picture> pictures = new();
        byte pictureSequence = 1;
        foreach (PictureInfo picInfo in request.PictureInfos)
        {
            pictures.Add(new(picInfo.BucketName, picInfo.Region, picInfo.FullObjectKey, pictureSequence++));
        }

        Submission submission = _domainService.CreateSubmissionWithFirstParagraph(submitterId, submitterName, request.TelNumber, request.Email, request.TextContent, pictures);
        _dbContext.Submissions.Add(submission);// 普通的增删改查而已，没必要非要写进仓储里，这就是洋葱架构的灵活性
        return Ok(submission.Id);
    }

    /// <summary>
    /// 对"待完善"问题进行补充
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Supplement(SupplementRequest request)
    {
        var validationResult = await _supplementValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        Submission? submission = await _dbContext.Submissions
            .Include(submission => submission.Paragraphs)
            .FirstOrDefaultAsync(submission => submission.Id == request.SubmissionId);
        if (submission == null)
        {
            return NotFound("问题不存在");
        }

        // 问题不属于当前登录的提交者则403
        Guid submitterId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (submission.SubmitterId != submitterId)
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

        bool supplementResult = _domainService.Supplement(submission, request.TextContent, pictures);
        if (supplementResult == false)
        {
            return BadRequest("该问题处于不可补充的状态");
        }

        return Ok();
    }

    /// <summary>
    /// 评价
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Evaluate(EvaluateRequest request)
    {
        var validationResult = await _evaluateValidator.ValidateAsync(request);
        if (validationResult.IsValid == false)
        {
            return BadRequest(validationResult.Errors.Select(error => error.ErrorMessage));
        }

        Submission? submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(submission => submission.Id == request.SubmissionId);
        if (submission == null)
        {
            return NotFound("问题不存在");
        }

        // 问题不属于当前登录的提交者则403
        Guid submitterId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (submission.SubmitterId != submitterId)
        {
            return Forbid();
        }

        _domainService.Evaluate(submission, request.IsSolved, request.Grade);
        return Ok();
    }

    /// <summary>
    /// 提交者获取一个自己的Submission的详细信息（包括每张图片的预签名Url）
    /// </summary>
    [HttpGet("{submissionId}")]
    public async Task<ActionResult<SubmissionVMforSubmitter>> GetSubmission(string submissionId)
    {
        Submission? submission = await _dbContext.Submissions
            .Include(submission => submission.Paragraphs)
            .FirstOrDefaultAsync(submission => submission.Id.ToString() == submissionId);
        if (submission == null)
        {
            return NotFound();
        }

        // Submission不属于请求发送者则禁止访问
        if (submission.SubmitterId.ToString() != this.User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Forbid();
        }

        List<ParagraphVM> paragraphVMs = new();
        foreach (Paragraph paragraph in submission.Paragraphs)
        {
            List<string> pictureUrls = await _repository.GetPictureUrlsOfParagraphAsync(submissionId, paragraph.SequenceInSubmission, 60);
            paragraphVMs.Add(new(paragraph.SequenceInSubmission, paragraph.CreationTime, paragraph.Sender.ToString(), paragraph.TextContent, pictureUrls));
            paragraphVMs = paragraphVMs.OrderBy(paragraphVM => paragraphVM.Sequence).ToList();
        }
        return new SubmissionVMforSubmitter(submission.SubmissionStatus, paragraphVMs);
    }

    [HttpGet]
    public async Task<ActionResult<GetEvaluationResponse>> GetEvaluation([RequiredGuid]Guid submissionId)
    {
        var evaluation = await _dbContext.Submissions
            .AsNoTracking()  // 性能优化：不进行不必要的跟踪
            .Select(submission => new { submission.Id, submission.SubmitterId, submission.SubmissionStatus, submission.Evaluation })  // 性能优化：只获取需要的列
            .FirstOrDefaultAsync(a => a.Id == submissionId);
        if (evaluation == null)
        {
            return NotFound("问题不存在");
        }

        // 问题不属于当前登录的提交者则403
        Guid submitterId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (evaluation.SubmitterId != submitterId)
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

    /// <summary>
    /// 获取提交者的全部Submission的简略信息，按照最后交互时间从晚到早排序
    /// <para>一个提交者的数据量不会太大，暂时不分页了</para>
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<List<SubmissionInfo>> GetSubmissionInfosOfSubmitter()
    {
        string id = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        List<SubmissionInfo> submissionInfos = await _repository.GetSubmissionInfosOfSubmitterAsync(id);
        return submissionInfos;
    }
}
