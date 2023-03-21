namespace SubmitService.Domain.Entities.Enums;

public enum SubmissionStatus
{
    /// <summary>
    /// 待分配
    /// </summary>
    ToBeAssigned,

    /// <summary>
    /// 待处理
    /// </summary>
    ToBeProcessed,

    /// <summary>
    /// 待完善（待补充）
    /// </summary>
    ToBeSupplemented,

    /// <summary>
    /// 待评价
    /// </summary>
    ToBeEvaluated,

    /// <summary>
    /// 已关闭
    /// </summary>
    Closed,
}
