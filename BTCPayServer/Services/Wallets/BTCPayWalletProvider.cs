using BTCPayServer.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Services.Wallets;

public class BTCPayWalletProvider
{
    public Logs Logs { get; }

    private readonly ExplorerClientProvider _Client;
    private readonly BTCPayNetworkProvider _NetworkProvider;
    private readonly IOptions<MemoryCacheOptions> _Options;
    public BTCPayWalletProvider(ExplorerClientProvider client,
                                IOptions<MemoryCacheOptions> memoryCacheOption,
                                Data.ApplicationDbContextFactory dbContextFactory,
                                BTCPayNetworkProvider networkProvider,
                                Logs logs)
    {
        ArgumentNullException.ThrowIfNull(client);
        Logs = logs;
        _Client = client;
        _NetworkProvider = networkProvider;
        _Options = memoryCacheOption;

        foreach (BTCPayNetwork network in networkProvider.GetAll().OfType<BTCPayNetwork>())
        {
            NBXplorer.ExplorerClient explorerClient = _Client.GetExplorerClient(network.CryptoCode);
            if (explorerClient == null)
            {
                continue;
            }

            _Wallets.Add(network.CryptoCode.ToUpperInvariant(), new BTCPayWallet(explorerClient, new MemoryCache(_Options), network, dbContextFactory, Logs));
        }
    }

    private readonly Dictionary<string, BTCPayWallet> _Wallets = new Dictionary<string, BTCPayWallet>();

    public BTCPayWallet GetWallet(BTCPayNetworkBase network)
    {
        ArgumentNullException.ThrowIfNull(network);
        return GetWallet(network.CryptoCode);
    }
    public BTCPayWallet GetWallet(string cryptoCode)
    {
        ArgumentNullException.ThrowIfNull(cryptoCode);
        _Wallets.TryGetValue(cryptoCode.ToUpperInvariant(), out BTCPayWallet result);
        return result;
    }

    public bool IsAvailable(BTCPayNetworkBase network)
    {
        return _Client.IsAvailable(network);
    }

    public IEnumerable<BTCPayWallet> GetWallets()
    {
        foreach (KeyValuePair<string, BTCPayWallet> w in _Wallets)
        {
            yield return w.Value;
        }
    }
}
