namespace SubmitService.Domain;

public interface ISubmitRepository
{
    Task<List<string>> GetPictureUrlsOfParagraphAsync(string submissionId, int paragraphSequence, long durationSeconds);
}
