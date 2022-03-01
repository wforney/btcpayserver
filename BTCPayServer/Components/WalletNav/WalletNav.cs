using BTCPayServer.Controllers;
using BTCPayServer.Data;
using BTCPayServer.Services.Wallets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BTCPayServer.Components.WalletNav;

public class WalletNav : ViewComponent
{
    private readonly BTCPayWalletProvider _walletProvider;
    private readonly UIWalletsController _walletsController;
    private readonly BTCPayNetworkProvider _networkProvider;

    public WalletNav(
        BTCPayWalletProvider walletProvider,
        BTCPayNetworkProvider networkProvider,
        UIWalletsController walletsController)
    {
        _walletProvider = walletProvider;
        _networkProvider = networkProvider;
        _walletsController = walletsController;
    }

    public async Task<IViewComponentResult> InvokeAsync(WalletId walletId)
    {
        StoreData store = ViewContext.HttpContext.GetStoreData();
        BTCPayNetwork network = _networkProvider.GetNetwork<BTCPayNetwork>(walletId.CryptoCode);
        BTCPayWallet wallet = _walletProvider.GetWallet(network);
        DerivationSchemeSettings derivation = store.GetDerivationSchemeSettings(_networkProvider, walletId.CryptoCode);
        var balance = await _walletsController.GetBalanceString(wallet, derivation.AccountDerivation);

        var vm = new WalletNavViewModel
        {
            WalletId = walletId,
            Network = network,
            Balance = balance,
            Label = derivation.Label ?? $"{store.StoreName} {walletId.CryptoCode} Wallet"
        };

        return View(vm);
    }
}
