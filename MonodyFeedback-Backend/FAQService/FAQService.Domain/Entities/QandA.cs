namespace FAQService.Domain.Entities;

public class QandA
{
    public Guid Id { get; init; }

    public Page Page { get; init; }

    public int Sequence { get; private set; }

    public string Question { get; private set; }

    public string Answer { get; private set; }

    private QandA() { }

    public QandA(int sequence, string question, string answer)
    {
        Sequence = sequence;
        Question = question;
        Answer = answer;
    }

    public QandA ChangeSequence(int value)
    {
        Sequence = value;
        return this;
    }

    public QandA ChangeQuestion(string value)
    {
        Question = value;
        return this;
    }

    public QandA ChangeAnswer(string value)
    {
        Answer = value;
        return this;
    }
}
