using BTCPayServer.Client.Models;

namespace BTCPayServer.Client;

public partial class BTCPayServerClient
{
    public virtual async Task<ServerInfoData> GetServerInfo(CancellationToken token = default)
    {
        System.Net.Http.HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest("api/v1/server/info"), token);
        return await HandleResponse<ServerInfoData>(response);
    }
}
