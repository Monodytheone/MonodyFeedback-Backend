using SubmitService.Domain.Entities.Enums;

namespace SubmitService.Domain.Entities;

public class Paragraph
{
    public Guid Id { get; init; }

    public Submission Submission { get; init; }

    public int SequenceInSubmission { get; init; }

    public DateTime CreationTime { get; init; }

    public Sender Sender { get; init; }

    public string TextContent { get; init; }

    public List<Picture> Pictures { get; init; } = new();


    private Paragraph() { }

    public Paragraph(Submission submission, Sender sender, string textContent, List<Picture> pictures)
    {
        //Id = Guid.NewGuid();
        SequenceInSubmission = submission.Paragraphs.Count() + 1;
        CreationTime = DateTime.Now;
        Sender = sender;
        TextContent = textContent;
        Pictures.AddRange(pictures);
    }
}
