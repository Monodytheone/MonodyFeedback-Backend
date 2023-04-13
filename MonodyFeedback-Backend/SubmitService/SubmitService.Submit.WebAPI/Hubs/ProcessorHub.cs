using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SubmitService.Submit.WebAPI.Hubs;

[Authorize(Roles = "processor")]
public class ProcessorHub : Hub
{
}
