using CommonInfrastructure.Filters.JWTRevoke;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SubmitService.Domain.Entities;
using SubmitService.Infrastructure;

namespace SubmitService.Submit.WebAPI.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = "submitter")]
public class TestController : ControllerBase
{
    private readonly SubmitDbContext _dbContext;

    public TestController(SubmitDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult> GenerateSubmissionAndInsert()
    {
        List<Picture> pictures = new List<Picture>
        {
            new("xxx", "yyy", "zzz"),
            new("aaa", "bbb", "ccc"),
            new("qwe", "asd", "zxc")
        };
        var newSubmisson = Submission.Create(Guid.NewGuid(), "小红", "15144444444", null, "永恒绿洲中的水面，跳跃落地时以及跳跃落地后的行走的前几步无法产生“脚印”效果，导致玩家体验不连贯，望解决", pictures);
        _dbContext.Submissions.Add(newSubmisson);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }
}
