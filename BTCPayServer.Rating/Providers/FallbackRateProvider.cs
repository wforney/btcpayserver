using BTCPayServer.Rating;

namespace BTCPayServer.Services.Rates;

public class FallbackRateProvider : IRateProvider
{
    private readonly IRateProvider[] _Providers;
    public FallbackRateProvider(IRateProvider[] providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _Providers = providers;
    }

    public async Task<PairRate[]> GetRatesAsync(CancellationToken cancellationToken)
    {
        foreach (IRateProvider p in _Providers)
        {
            try
            {
                return await p.GetRatesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) { Exceptions.Add(ex); }
        }
        return Array.Empty<PairRate>();
    }

    public List<Exception> Exceptions { get; set; } = new List<Exception>();
}
