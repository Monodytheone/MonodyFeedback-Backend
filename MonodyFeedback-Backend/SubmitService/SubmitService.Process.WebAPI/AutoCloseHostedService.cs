using SubmitService.Domain;

namespace SubmitService.Process.WebAPI;

/// <summary>
/// 自动结束长时间未评价未补充的Submission的托管服务
/// </summary>
public class AutoCloseHostedService : BackgroundService
{
    private readonly IServiceScope _serviceScope;
    private readonly SubmitDomainService _domainService;

    public AutoCloseHostedService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        IServiceProvider serviceProvider = _serviceScope.ServiceProvider;

        // 用服务定位器的方式要来需要的服务：
        _domainService = serviceProvider.GetRequiredService<SubmitDomainService>();
    }

    public override void Dispose()
    {
        _serviceScope.Dispose();  // 在托管服务Dispose时，顺便把这个Scope也Dispose掉
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool closeUnevaluated = true;
        while (stoppingToken.IsCancellationRequested == false)
        {
            // 为了看效果，执行间隔和超期时间设得比较短（5分钟执行一次，20分钟未评价/未完善即自动关闭）
            // 实际生产中可以设为半小时执行一次（或随机设置执行间隔防止数据库压力“雪崩”），超期时间两天
            try
            {
                if (closeUnevaluated)
                {
                    await _domainService.CloseSubmissionsWaitLong_InToBeEvaluatedStatus_Async(1200);
                }
                else
                {
                    await _domainService.CloseSubmissionsWaitLong_InToBeSupplementStatus_Async(1200);
                }
            }
            catch(Exception ex) 
            {
                await Console.Out.WriteLineAsync($"托管服务出错辣：{ex}");
                // 需要记录日志
            }
            finally
            {
                closeUnevaluated = !closeUnevaluated;
                await Task.Delay(TimeSpan.FromSeconds(300));
            }
        }
    }
}
