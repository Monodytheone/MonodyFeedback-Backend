using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SubmitService.Process.WebAPI.Hubs;

[Authorize(Roles = "submitter")]
public class SubmitterHub : Hub
{
}
