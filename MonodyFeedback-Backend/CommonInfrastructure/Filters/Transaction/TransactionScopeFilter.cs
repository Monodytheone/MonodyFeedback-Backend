using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Transactions;

namespace CommonInfrastructure.Filters.Transaction;

/// <summary>
///  自动启用事务管理的筛选器
/// </summary>
public class TransactionScopeFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. 判断Action方法是否标注了NotTransactionalAttribute
        bool hasNotTransactionalAttribute = false;
        if(context.ActionDescriptor is ControllerActionDescriptor)
        {
            ControllerActionDescriptor actionDesc = (ControllerActionDescriptor)context.ActionDescriptor;
            hasNotTransactionalAttribute = actionDesc.MethodInfo.IsDefined(typeof(NotTransactionalAttribute), false);
        }

        if(hasNotTransactionalAttribute)
        {
            await next();
            return;
        }

        // 2. 创建TransactionScope对象
        using var txScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var result = await next();  // 执行Action方法

        // 3. 如果Action方法没有出现异常，则最终提交事务
        if (result.Exception == null)
        {
            txScope.Complete();
        }
    }
}
