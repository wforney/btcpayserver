using BTCPayServer.Rating;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Services.Rates;

public class BitbankRateProvider : IRateProvider
{
    private readonly HttpClient _httpClient;
    public BitbankRateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<PairRate[]> GetRatesAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.GetAsync("https://public.bitbank.cc/tickers", cancellationToken);
        JObject jobj = await response.Content.ReadAsAsync<JObject>(cancellationToken);
        JToken data = jobj.ContainsKey("data") ? jobj["data"] : null;
        if (jobj["success"]?.Value<int>() != 1)
        {
            var errorCode = data is null ? "Unknown" : data["code"].Value<string>();
            throw new Exception(
                $"BitBank Rates API Error: {errorCode}. See https://github.com/bitbankinc/bitbank-api-docs/blob/master/errors.md for more details.");
        }
        return ((data as JArray) ?? new JArray())
            .Select(item => new PairRate(CurrencyPair.Parse(item["pair"].ToString()), CreateBidAsk(item as JObject)))
            .ToArray();
    }

    private static BidAsk CreateBidAsk(JObject o)
    {
        var buy = o["buy"].Value<decimal>();
        var sell = o["sell"].Value<decimal>();
        // Bug from their API (https://github.com/btcpayserver/btcpayserver/issues/741)
        return buy < sell ? new BidAsk(buy, sell) : new BidAsk(sell, buy);
    }
}
