using System.Text.Json;

namespace Microsoft.AspNetCore.Identity;

public static class IdentityHelper
{
    public static async Task CheckIdentityResultAsync(this Task<IdentityResult> taskIdentityResult)
    {
        IdentityResult identityResult = await taskIdentityResult;
        if (identityResult.Succeeded == false)
        {
            throw new Exception(JsonSerializer.Serialize(identityResult.Errors));
        }
    }
}
