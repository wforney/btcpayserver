using BTCPayServer.Client.Models;

namespace BTCPayServer.Client;

public partial class BTCPayServerClient
{
    public virtual async Task<ApiHealthData> GetHealth(CancellationToken token = default)
    {
        System.Net.Http.HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest("api/v1/health"), token);
        return await HandleResponse<ApiHealthData>(response);
    }
}
