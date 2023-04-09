namespace CommonInfrastructure.Filters.Transaction;

/// <summary>
/// 标注Action方法不启用事务控制
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NotTransactionalAttribute : Attribute
{
}
