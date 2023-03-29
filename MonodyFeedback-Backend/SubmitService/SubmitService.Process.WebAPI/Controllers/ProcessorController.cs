using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubmitService.Domain;
using SubmitService.Infrastructure;
using Zack.ASPNETCore;

namespace SubmitService.Process.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [Authorize(Roles = "processor")]
    [UnitOfWork(typeof(SubmitDbContext))]
    [ApiController]
    public class ProcessorController : ControllerBase
    {
        private readonly SubmitDomainService _domainService;
        private readonly ISubmitRepository _repository;

        public ProcessorController(SubmitDomainService domainService, ISubmitRepository submitRepository)
        {
            _domainService = domainService;
            _repository = submitRepository;
        }
    }
}
