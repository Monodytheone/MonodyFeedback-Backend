using SubmitService.Domain.Entities;

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

}
