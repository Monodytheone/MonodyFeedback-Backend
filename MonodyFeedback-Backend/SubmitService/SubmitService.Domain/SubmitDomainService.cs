using SubmitService.Domain.Entities;

namespace SubmitService.Domain;

public class SubmitDomainService
{
    private readonly ISubmitRepository _submitRepository;

    public SubmitDomainService(ISubmitRepository submitRepository)
    {
        _submitRepository = submitRepository;
    }

    //public Submission CreateSubmissionWithFirstParagraph()
    //{

    //}
}
