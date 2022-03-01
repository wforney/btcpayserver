using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Services;

namespace BTCPayServer.Hosting;

public class BlockExplorerLinkStartupTask : IStartupTask
{
    private readonly SettingsRepository _settingsRepository;
    private readonly BTCPayNetworkProvider _btcPayNetworkProvider;

    public BlockExplorerLinkStartupTask(SettingsRepository settingsRepository,
        BTCPayNetworkProvider btcPayNetworkProvider)
    {
        _settingsRepository = settingsRepository;
        _btcPayNetworkProvider = btcPayNetworkProvider;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        PoliciesSettings settings = await _settingsRepository.GetSettingAsync<PoliciesSettings>();
        if (settings?.BlockExplorerLinks?.Any() is true)
        {
            SetLinkOnNetworks(settings.BlockExplorerLinks, _btcPayNetworkProvider);
        }
    }

    public static void SetLinkOnNetworks(List<PoliciesSettings.BlockExplorerOverrideItem> links,
        BTCPayNetworkProvider networkProvider)
    {
        IEnumerable<BTCPayNetworkBase> networks = networkProvider.GetAll();
        foreach (BTCPayNetworkBase network in networks)
        {
            PoliciesSettings.BlockExplorerOverrideItem overrideLink = links.SingleOrDefault(item =>
                item.CryptoCode.Equals(network.CryptoCode, StringComparison.InvariantCultureIgnoreCase));
            network.BlockExplorerLink = overrideLink?.Link ?? network.BlockExplorerLinkDefault;

        }
    }
}
