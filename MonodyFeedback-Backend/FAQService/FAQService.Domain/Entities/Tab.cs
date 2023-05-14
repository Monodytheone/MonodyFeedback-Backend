namespace FAQService.Domain.Entities;

public class Tab
{
    public Guid Id { get; init; }

    public string Name { get; private set; }

    public int Sequence { get; private set; }

    public List<Page> Pages { get; set; } = new();

    private Tab() { }

    public Tab(string name, int sequence)
    {
        Name = name;
        Sequence = sequence;
    }

    public Tab ChangeName(string value)
    {
        Name = value;
        return this;
    }

    public Tab ChangeSequence(int sequence)
    {
        Sequence = sequence;
        return this;
    }
}
