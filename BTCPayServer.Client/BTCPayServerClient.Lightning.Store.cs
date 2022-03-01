using BTCPayServer.Client.Models;

namespace BTCPayServer.Client;

public partial class BTCPayServerClient
{
    public virtual async Task<LightningNodeInformationData> GetLightningNodeInfo(string storeId, string cryptoCode,
        CancellationToken token = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/info",
                method: HttpMethod.Get), token);
        return await HandleResponse<LightningNodeInformationData>(response);
    }

    public virtual async Task ConnectToLightningNode(string storeId, string cryptoCode, ConnectToNodeRequest request,
        CancellationToken token = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/connect", bodyPayload: request,
                method: HttpMethod.Post), token);
        await HandleResponse(response);
    }

    public virtual async Task<IEnumerable<LightningChannelData>> GetLightningNodeChannels(string storeId, string cryptoCode,
        CancellationToken token = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/channels",
                method: HttpMethod.Get), token);
        return await HandleResponse<IEnumerable<LightningChannelData>>(response);
    }

    public virtual async Task OpenLightningChannel(string storeId, string cryptoCode, OpenLightningChannelRequest request,
        CancellationToken token = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/channels", bodyPayload: request,
                method: HttpMethod.Post), token);
        await HandleResponse(response);
    }

    public virtual async Task<string> GetLightningDepositAddress(string storeId, string cryptoCode,
        CancellationToken token = default)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/address", method: HttpMethod.Post),
            token);
        return await HandleResponse<string>(response);
    }

    public virtual async Task PayLightningInvoice(string storeId, string cryptoCode, PayLightningInvoiceRequest request,
        CancellationToken token = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/invoices/pay", bodyPayload: request,
                method: HttpMethod.Post), token);
        await HandleResponse(response);
    }

    public virtual async Task<LightningInvoiceData> GetLightningInvoice(string storeId, string cryptoCode,
        string invoiceId, CancellationToken token = default)
    {
        if (invoiceId == null)
        {
            throw new ArgumentNullException(nameof(invoiceId));
        }

        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/invoices/{invoiceId}",
                method: HttpMethod.Get), token);
        return await HandleResponse<LightningInvoiceData>(response);
    }

    public virtual async Task<LightningInvoiceData> CreateLightningInvoice(string storeId, string cryptoCode,
        CreateLightningInvoiceRequest request, CancellationToken token = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        HttpResponseMessage response = await _httpClient.SendAsync(
            CreateHttpRequest($"api/v1/stores/{storeId}/lightning/{cryptoCode}/invoices", bodyPayload: request,
                method: HttpMethod.Post), token);
        return await HandleResponse<LightningInvoiceData>(response);
    }
}
