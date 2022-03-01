using System.Globalization;
using BTCPayServer.Rating;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Services.Rates;

public class RipioExchangeProvider : IRateProvider
{
    private readonly HttpClient _httpClient;
    public RipioExchangeProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
    }
    public async Task<PairRate[]> GetRatesAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.GetAsync("https://api.exchange.ripio.com/api/v1/rate/all/", cancellationToken);
        JArray jarray = await response.Content.ReadAsAsync<JArray>(cancellationToken);
        return jarray
            .Children<JObject>()
            .Select(jobj => ParsePair(jobj))
            .Where(p => p != null)
            .ToArray();
    }

    private PairRate ParsePair(JObject jobj)
    {
        var pair = CurrencyPair.Parse(jobj["pair"].Value<string>());
        var bid = decimal.Parse(jobj["bid"].Value<string>(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
        var ask = decimal.Parse(jobj["ask"].Value<string>(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture);
        if (bid > ask)
        {
            return null;
        }

        return new PairRate(pair, new BidAsk(bid, ask));
    }
}
