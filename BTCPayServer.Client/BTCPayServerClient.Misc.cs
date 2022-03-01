using BTCPayServer.Client.Models;

namespace BTCPayServer.Client;

public partial class BTCPayServerClient
{
    public virtual async Task<PermissionMetadata[]> GetPermissionMetadata(CancellationToken token = default)
    {
        System.Net.Http.HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest("misc/permissions"), token);
        return await HandleResponse<PermissionMetadata[]>(response);
    }
    public virtual async Task<Language[]> GetAvailableLanguages(CancellationToken token = default)
    {
        System.Net.Http.HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest("misc/lang"), token);
        return await HandleResponse<Language[]>(response);
    }
}
