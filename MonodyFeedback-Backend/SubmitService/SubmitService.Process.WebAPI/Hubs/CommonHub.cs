using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SubmitService.Process.WebAPI.Hubs;

[Authorize(Roles = "processor,submitter")]
public class CommonHub : Hub
{
}
