namespace QuestionnaireService.Domain.Entities;

public class Question
{
    public Guid Id { get; init; }

    public Questionnaire Questionnaire { get; init; }

    public string QuestionName { get; init; }

    public int Sequence { get; private set; }

    /// <summary>
    /// 选项数组
    /// </summary>
    public List<string> Options { get; private set; }

    public Question(string questionName, int sequence)
    {
        QuestionName = questionName;
        Sequence = sequence;
    }
}
