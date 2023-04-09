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
            try
            {
                if (closeUnevaluated)
                {
                    await _domainService.CloseSubmissionsWaitLong_InToBeEvaluatedStatus_Async();
                }
                else
                {
                    await _domainService.CloseSubmissionsWaitLong_InToBeSupplementStatus_Async();
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
                await Task.Delay(TimeSpan.FromSeconds(1800));  // 每半小时执行一次
            }
        }
    }
}
