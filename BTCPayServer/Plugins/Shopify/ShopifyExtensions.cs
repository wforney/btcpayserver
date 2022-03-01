using BTCPayServer.Data;
using BTCPayServer.Plugins.Shopify.Models;
using NBitcoin;
using NBXplorer;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Plugins.Shopify;

public static class ShopifyExtensions
{
    public const string StoreBlobKey = "shopify";
    public static ShopifyApiClientCredentials CreateShopifyApiCredentials(this ShopifySettings shopify)
    {
        return new ShopifyApiClientCredentials
        {
            ShopName = shopify.ShopName,
            ApiKey = shopify.ApiKey,
            ApiPassword = shopify.Password
        };
    }

    public static ShopifySettings GetShopifySettings(this StoreBlob storeBlob)
    {
        if (storeBlob.AdditionalData.TryGetValue(StoreBlobKey, out JToken rawS))
        {
            if (rawS is JObject rawObj)
            {
                return new Serializer(null).ToObject<ShopifySettings>(rawObj);
            }
            else if (rawS.Type == JTokenType.String)
            {
                return new Serializer(null).ToObject<ShopifySettings>(rawS.Value<string>());
            }

        }

        return null;
    }
    public static void SetShopifySettings(this StoreBlob storeBlob, ShopifySettings settings)
    {
        if (settings is null)
        {
            storeBlob.AdditionalData.Remove(StoreBlobKey);
        }
        else
        {
            storeBlob.AdditionalData.AddOrReplace(StoreBlobKey, new Serializer(null).ToString(settings));
        }
    }
}
