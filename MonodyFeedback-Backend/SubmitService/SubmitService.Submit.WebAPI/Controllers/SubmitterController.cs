using CommonInfrastructure.TencentCOS;
using CommonInfrastructure.TencentCOS.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubmitService.Domain;
using SubmitService.Domain.Entities;
using SubmitService.Infrastructure;
using SubmitService.Submit.WebAPI.Controllers.Requests;
using SubmitService.Submit.WebAPI.Controllers.Responses;
using System.Net;
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

    public SubmitterController(SubmitDomainService submitDomainService, ISubmitRepository repository, IValidator<SubmitRequest> submitValidator, SubmitDbContext dbContext, IOptionsSnapshot<COSPictureOptions> pictureOptions, COSService cosService)
    {
        _domainService = submitDomainService;
        _repository = repository;
        _submitValidator = submitValidator;
        _dbContext = dbContext;
        _pictureOptions = pictureOptions;
        _cosService = cosService;
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
        }
        return new SubmissionVMforSubmitter(submission.SubmissionStatus.ToString(), paragraphVMs);
    }
}
