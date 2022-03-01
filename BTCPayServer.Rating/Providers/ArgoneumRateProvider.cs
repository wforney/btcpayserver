using BTCPayServer.Rating;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Services.Rates;

public class ArgoneumRateProvider : IRateProvider
{
    private readonly HttpClient _httpClient;
    public ArgoneumRateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<PairRate[]> GetRatesAsync(CancellationToken cancellationToken)
    {
        // Example result: AGM to BTC rate: {"agm":5000000.000000}
        HttpResponseMessage response = await _httpClient.GetAsync("https://rates.argoneum.net/rates/btc", cancellationToken);
        JObject jobj = await response.Content.ReadAsAsync<JObject>(cancellationToken);
        var value = jobj["agm"].Value<decimal>();
        return new[] { new PairRate(new CurrencyPair("BTC", "AGM"), new BidAsk(value)) };
    }
}
