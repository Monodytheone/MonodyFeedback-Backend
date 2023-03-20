namespace SubmitService.Domain.Entities.ValueObjects;

/// <summary>
/// 用户评价--Submission中的值对象
/// </summary>
public class Evaluation
{
    /// <summary>
    /// 是否已解决
    /// </summary>
    public bool IsSolved { get; init; }

    /// <summary>
    /// 评分
    /// </summary>
    public byte Grade { get; init; }

    public Evaluation(bool isSolved, byte grade)
    {
        IsSolved = isSolved;
        Grade = grade;
    }

    private Evaluation() { }
}
