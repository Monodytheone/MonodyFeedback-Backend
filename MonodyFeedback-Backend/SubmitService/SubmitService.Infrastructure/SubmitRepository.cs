using CommonInfrastructure.TencentCOS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubmitService.Domain;
using SubmitService.Domain.Entities;

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
}
