using System.Text;
using BTCPayServer.Client.Models;
using Newtonsoft.Json;

namespace BTCPayServer.Data;

public class AuthorizedWebhookEvents
{
    public bool Everything { get; set; }

    [JsonProperty(ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public WebhookEventType[] SpecificEvents { get; set; } = Array.Empty<WebhookEventType>();
    public bool Match(WebhookEventType evt)
    {
        return Everything || SpecificEvents.Contains(evt);
    }
}


public class WebhookDeliveryBlob
{
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public WebhookDeliveryStatus Status { get; set; }
    public int? HttpCode { get; set; }
    public string ErrorMessage { get; set; }
    public byte[] Request { get; set; }
    public T ReadRequestAs<T>()
    {
        return JsonConvert.DeserializeObject<T>(UTF8Encoding.UTF8.GetString(Request), HostedServices.WebhookSender.DefaultSerializerSettings);
    }
}
public class WebhookBlob
{
    public string Url { get; set; }
    public bool Active { get; set; } = true;
    public string Secret { get; set; }
    public bool AutomaticRedelivery { get; set; }
    public AuthorizedWebhookEvents AuthorizedEvents { get; set; }
}
public static class WebhookDataExtensions
{
    public static WebhookBlob GetBlob(this WebhookData webhook)
    {
        return JsonConvert.DeserializeObject<WebhookBlob>(Encoding.UTF8.GetString(webhook.Blob));
    }
    public static void SetBlob(this WebhookData webhook, WebhookBlob blob)
    {
        webhook.Blob = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(blob));
    }
    public static WebhookDeliveryBlob GetBlob(this WebhookDeliveryData webhook)
    {
        return JsonConvert.DeserializeObject<WebhookDeliveryBlob>(ZipUtils.Unzip(webhook.Blob), HostedServices.WebhookSender.DefaultSerializerSettings);
    }
    public static void SetBlob(this WebhookDeliveryData webhook, WebhookDeliveryBlob blob)
    {
        webhook.Blob = ZipUtils.Zip(JsonConvert.SerializeObject(blob, Formatting.None, HostedServices.WebhookSender.DefaultSerializerSettings));
    }
}
