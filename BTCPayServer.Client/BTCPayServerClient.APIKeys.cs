using BTCPayServer.Client.Models;

namespace BTCPayServer.Client;

public partial class BTCPayServerClient
{
    public virtual async Task<ApiKeyData> GetCurrentAPIKeyInfo(CancellationToken token = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest("api/v1/api-keys/current"), token);
        return await HandleResponse<ApiKeyData>(response);
    }

    public virtual async Task<ApiKeyData> CreateAPIKey(CreateApiKeyRequest request, CancellationToken token = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest("api/v1/api-keys", bodyPayload: request, method: HttpMethod.Post), token);
        return await HandleResponse<ApiKeyData>(response);
    }

    public virtual async Task RevokeCurrentAPIKeyInfo(CancellationToken token = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest("api/v1/api-keys/current", null, HttpMethod.Delete), token);
        await HandleResponse(response);
    }

    public virtual async Task RevokeAPIKey(string apikey, CancellationToken token = default)
    {
        if (apikey == null)
        {
            throw new ArgumentNullException(nameof(apikey));
        }

        HttpResponseMessage response = await _httpClient.SendAsync(CreateHttpRequest($"api/v1/api-keys/{apikey}", null, HttpMethod.Delete), token);
        await HandleResponse(response);
    }
}
