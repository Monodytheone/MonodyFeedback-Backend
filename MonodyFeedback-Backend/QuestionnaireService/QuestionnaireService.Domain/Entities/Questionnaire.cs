namespace QuestionnaireService.Domain.Entities;

public class Questionnaire
{
    public Guid Id { get; init; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public List<Question> Question { get; private set; } = new();

    private Questionnaire() { }

    public Questionnaire(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        IsActive = true;
    }
}
