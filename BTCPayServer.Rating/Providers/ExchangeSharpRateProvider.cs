using System.Collections.Concurrent;
using BTCPayServer.Rating;
using ExchangeSharp;

namespace BTCPayServer.Services.Rates;

public class ExchangeSharpRateProvider<T> : IRateProvider where T : ExchangeAPI
{
    private readonly HttpClient _httpClient;
    public ExchangeSharpRateProvider(HttpClient httpClient, bool reverseCurrencyPair = false)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ReverseCurrencyPair = reverseCurrencyPair;
        _httpClient = httpClient;
    }

    public bool ReverseCurrencyPair
    {
        get; set;
    }

    public async Task<PairRate[]> GetRatesAsync(CancellationToken cancellationToken)
    {
        await new SynchronizationContextRemover();

        T exchangeAPI = await Create();
        exchangeAPI.RequestMaker = new HttpClientRequestMaker(exchangeAPI, _httpClient, cancellationToken);
        IEnumerable<KeyValuePair<string, ExchangeTicker>> rates = await exchangeAPI.GetTickersAsync();

        IEnumerable<Task<PairRate>> exchangeRateTasks = rates
            .Where(t => t.Value.Ask != 0m && t.Value.Bid != 0m)
            .Select(t => CreateExchangeRate(exchangeAPI, t));

        PairRate[] exchangeRates = await Task.WhenAll(exchangeRateTasks);

        return exchangeRates
            .Where(t => t != null)
            .ToArray();
    }

    private static async Task<T> Create() => (T)await ExchangeAPI.GetExchangeAPIAsync<T>();

    private static Task<TExchangeAPI> Create<TExchangeAPI>() where TExchangeAPI : new() => Task.FromResult(new TExchangeAPI());

    // ExchangeSymbolToGlobalSymbol throws exception which would kill perf
    private readonly ConcurrentDictionary<string, string> notFoundSymbols = new();
    private async Task<PairRate> CreateExchangeRate(T exchangeAPI, KeyValuePair<string, ExchangeTicker> ticker)
    {
        if (notFoundSymbols.TryGetValue(ticker.Key, out _))
        {
            return null;
        }

        try
        {
            var tickerName = await exchangeAPI.ExchangeMarketSymbolToGlobalMarketSymbolAsync(ticker.Key);
            if (!CurrencyPair.TryParse(tickerName, out CurrencyPair pair))
            {
                notFoundSymbols.TryAdd(ticker.Key, ticker.Key);
                return null;
            }
            if (ReverseCurrencyPair)
            {
                pair = new CurrencyPair(pair.Right, pair.Left);
            }

            return new PairRate(pair, new BidAsk(ticker.Value.Bid, ticker.Value.Ask));
        }
        catch (ArgumentException)
        {
            notFoundSymbols.TryAdd(ticker.Key, ticker.Key);
            return null;
        }
    }
}
