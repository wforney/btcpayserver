using BTCPayServer.Logging;
using BTCPayServer.Services;
using BTCPayServer.Services.Rates;
using NBitcoin;
using Newtonsoft.Json;

namespace BTCPayServer.HostedServices;

public class RatesHostedService : BaseAsyncService
{
    public class ExchangeRatesCache
    {
        [JsonConverter(typeof(NBitcoin.JsonConverters.DateTimeToUnixTimeConverter))]
        public DateTimeOffset Created { get; set; }
        public List<BackgroundFetcherState> States { get; set; }
        public override string ToString()
        {
            return "";
        }
    }
    private readonly SettingsRepository _SettingsRepository;
    private readonly RateProviderFactory _RateProviderFactory;

    public RatesHostedService(SettingsRepository repo,
                              RateProviderFactory rateProviderFactory,
                              Logs logs) : base(logs)
    {
        _SettingsRepository = repo;
        _RateProviderFactory = rateProviderFactory;
    }

    internal override Task[] InitializeTasks()
    {
        return new Task[]
        {
                CreateLoopTask(RefreshRates)
        };
    }

    private bool IsStillUsed(BackgroundFetcherRateProvider fetcher)
    {
        return fetcher.LastRequested is DateTimeOffset v &&
               DateTimeOffset.UtcNow - v < TimeSpan.FromDays(1.0);
    }

    private IEnumerable<(string ExchangeName, BackgroundFetcherRateProvider Fetcher)> GetStillUsedProviders()
    {
        foreach (KeyValuePair<string, IRateProvider> kv in _RateProviderFactory.Providers)
        {
            if (kv.Value is BackgroundFetcherRateProvider fetcher && IsStillUsed(fetcher))
            {
                yield return (kv.Key, fetcher);
            }
        }
    }

    private async Task RefreshRates()
    {
        (string ExchangeName, BackgroundFetcherRateProvider Fetcher)[] usedProviders = GetStillUsedProviders().ToArray();
        if (usedProviders.Length == 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), Cancellation);
            return;
        }
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(Cancellation))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(20.0));
            try
            {
                await Task.WhenAll(usedProviders
                                .Select(p => p.Fetcher.UpdateIfNecessary(timeout.Token).ContinueWith(t =>
                                {
                                    if (t.Result.Exception != null)
                                    {
                                        Logs.PayServer.LogWarning($"Error while contacting exchange {p.ExchangeName}: {t.Result.Exception.Message}");
                                    }
                                }, TaskScheduler.Default))
                                .ToArray()).WithCancellation(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
            }
            if (_LastCacheDate is DateTimeOffset lastCache)
            {
                if (DateTimeOffset.UtcNow - lastCache > TimeSpan.FromMinutes(8.0))
                {
                    await SaveRateCache();
                }
            }
            else
            {
                await SaveRateCache();
            }
        }
        await Task.Delay(TimeSpan.FromSeconds(30), Cancellation);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await TryLoadRateCache();
        await base.StartAsync(cancellationToken);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveRateCache();
        await base.StopAsync(cancellationToken);
    }

    private async Task TryLoadRateCache()
    {
        try
        {
            ExchangeRatesCache cache = await _SettingsRepository.GetSettingAsync<ExchangeRatesCache>();
            if (cache != null)
            {
                _LastCacheDate = cache.Created;
                var stateByExchange = cache.States.ToDictionary(o => o.ExchangeName);
                foreach (KeyValuePair<string, IRateProvider> provider in _RateProviderFactory.Providers)
                {
                    if (stateByExchange.TryGetValue(provider.Key, out BackgroundFetcherState state) &&
                        provider.Value is BackgroundFetcherRateProvider fetcher)
                    {
                        fetcher.LoadState(state);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logs.PayServer.LogWarning(ex, "Warning: Error while trying to load rates from cache");
        }
    }

    private DateTimeOffset? _LastCacheDate;
    private async Task SaveRateCache()
    {
        var cache = new ExchangeRatesCache
        {
            Created = DateTimeOffset.UtcNow
        };
        _LastCacheDate = cache.Created;

        (string ExchangeName, BackgroundFetcherRateProvider Fetcher)[] usedProviders = GetStillUsedProviders().ToArray();
        cache.States = new List<BackgroundFetcherState>(usedProviders.Length);
        foreach ((string ExchangeName, BackgroundFetcherRateProvider Fetcher) provider in usedProviders)
        {
            BackgroundFetcherState state = provider.Fetcher.GetState();
            state.ExchangeName = provider.ExchangeName;
            cache.States.Add(state);
        }
        await _SettingsRepository.UpdateSetting(cache);
    }
}
