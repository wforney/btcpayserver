using BTCPayServer.Controllers;
using BTCPayServer.Data;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Services.Apps;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BTCPayServer.Components.MainNav;

public class MainNav : ViewComponent
{
    private readonly AppService _appService;
    private readonly StoreRepository _storeRepo;
    private readonly UIStoresController _storesController;
    private readonly BTCPayNetworkProvider _networkProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PaymentMethodHandlerDictionary _paymentMethodHandlerDictionary;

    public MainNav(
        AppService appService,
        StoreRepository storeRepo,
        UIStoresController storesController,
        BTCPayNetworkProvider networkProvider,
        UserManager<ApplicationUser> userManager,
        PaymentMethodHandlerDictionary paymentMethodHandlerDictionary)
    {
        _storeRepo = storeRepo;
        _appService = appService;
        _userManager = userManager;
        _networkProvider = networkProvider;
        _storesController = storesController;
        _paymentMethodHandlerDictionary = paymentMethodHandlerDictionary;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        StoreData store = ViewContext.HttpContext.GetStoreData();
        var vm = new MainNavViewModel { Store = store };
#if ALTCOINS
            vm.AltcoinsBuild = true;
#endif
        if (store != null)
        {
            StoreBlob storeBlob = store.GetStoreBlob();

            // Wallets
            _storesController.AddPaymentMethods(store, storeBlob,
                out List<StoreDerivationScheme> derivationSchemes, out List<StoreLightningNode> lightningNodes);
            vm.DerivationSchemes = derivationSchemes;
            vm.LightningNodes = lightningNodes;

            // Apps
            Models.AppViewModels.ListAppsViewModel.ListAppViewModel[] apps = await _appService.GetAllApps(UserId, false, store.Id);
            vm.Apps = apps.Select(a => new StoreApp
            {
                Id = a.Id,
                AppName = a.AppName,
                AppType = a.AppType,
                IsOwner = a.IsOwner
            }).ToList();
        }

        return View(vm);
    }

    private string UserId => _userManager.GetUserId(HttpContext.User);
}
