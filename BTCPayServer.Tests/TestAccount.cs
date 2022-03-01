using System.Text;
using BTCPayServer.BIP78.Sender;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Controllers;
using BTCPayServer.Data;
using BTCPayServer.Events;
using BTCPayServer.Lightning;
using BTCPayServer.Models.AccountViewModels;
using BTCPayServer.Models.StoreViewModels;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Payments.PayJoin.Sender;
using BTCPayServer.Services;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.Payment;
using NBitpayClient;
using NBXplorer.DerivationStrategy;
using NBXplorer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace BTCPayServer.Tests;

public class TestAccount
{
    private readonly ServerTester parent;

    public TestAccount(ServerTester parent)
    {
        this.parent = parent;
        BitPay = new Bitpay(new Key(), parent.PayTester.ServerUri);
    }

    public void GrantAccess(bool isAdmin = false)
    {
        GrantAccessAsync(isAdmin).GetAwaiter().GetResult();
    }

    public async Task MakeAdmin(bool isAdmin = true)
    {
        UserManager<ApplicationUser> userManager = parent.PayTester.GetService<UserManager<ApplicationUser>>();
        ApplicationUser u = await userManager.FindByIdAsync(UserId);
        if (isAdmin)
        {
            await userManager.AddToRoleAsync(u, Roles.ServerAdmin);
        }
        else
        {
            await userManager.RemoveFromRoleAsync(u, Roles.ServerAdmin);
        }

        IsAdmin = true;
    }

    public Task<BTCPayServerClient> CreateClient()
    {
        return Task.FromResult(new BTCPayServerClient(parent.PayTester.ServerUri, RegisterDetails.Email,
            RegisterDetails.Password));
    }

    public async Task<BTCPayServerClient> CreateClient(params string[] permissions)
    {
        UIManageController manageController = parent.PayTester.GetController<UIManageController>(UserId, StoreId, IsAdmin);
        RedirectToActionResult x = Assert.IsType<RedirectToActionResult>(await manageController.AddApiKey(
            new UIManageController.AddApiKeyViewModel()
            {
                PermissionValues = permissions.Select(s =>
                {
                    Permission.TryParse(s, out Permission p);
                    return p;
                }).GroupBy(permission => permission.Policy).Select(p =>
                {
                    var stores = p.Where(permission => !string.IsNullOrEmpty(permission.Scope))
                        .Select(permission => permission.Scope).ToList();
                    return new UIManageController.AddApiKeyViewModel.PermissionValueItem()
                    {
                        Permission = p.Key,
                        Forbidden = false,
                        StoreMode = stores.Any() ? UIManageController.AddApiKeyViewModel.ApiKeyStoreMode.Specific : UIManageController.AddApiKeyViewModel.ApiKeyStoreMode.AllStores,
                        SpecificStores = stores,
                        Value = true
                    };
                }).ToList()
            }));
        Abstractions.Models.StatusMessageModel statusMessage = manageController.TempData.GetStatusMessageModel();
        Assert.NotNull(statusMessage);
        var str = "<code class='alert-link'>";
        var apiKey = statusMessage.Html.Substring(statusMessage.Html.IndexOf(str) + str.Length);
        apiKey = apiKey.Substring(0, apiKey.IndexOf("</code>"));
        return new BTCPayServerClient(parent.PayTester.ServerUri, apiKey);
    }

    public void Register(bool isAdmin = false)
    {
        RegisterAsync(isAdmin).GetAwaiter().GetResult();
    }

    public async Task GrantAccessAsync(bool isAdmin = false)
    {
        await RegisterAsync(isAdmin);
        await CreateStoreAsync();
        UIStoresController store = GetController<UIStoresController>();
        PairingCode pairingCode = BitPay.RequestClientAuthorization("test", Facade.Merchant);
        Assert.IsType<ViewResult>(await store.RequestPairing(pairingCode.ToString()));
        await store.Pair(pairingCode.ToString(), StoreId);
    }

    public BTCPayServerClient CreateClientFromAPIKey(string apiKey)
    {
        return new BTCPayServerClient(parent.PayTester.ServerUri, apiKey);
    }

    public void CreateStore()
    {
        CreateStoreAsync().GetAwaiter().GetResult();
    }

    public async Task SetNetworkFeeMode(NetworkFeeMode mode)
    {
        await ModifyPayment(payment =>
        {
            payment.NetworkFeeMode = mode;
        });
    }

    public async Task ModifyPayment(Action<GeneralSettingsViewModel> modify)
    {
        UIStoresController storeController = GetController<UIStoresController>();
        IActionResult response = storeController.GeneralSettings();
        GeneralSettingsViewModel settings = (GeneralSettingsViewModel)((ViewResult)response).Model;
        modify(settings);
        await storeController.GeneralSettings(settings);
    }

    public async Task ModifyWalletSettings(Action<WalletSettingsViewModel> modify)
    {
        UIStoresController storeController = GetController<UIStoresController>();
        IActionResult response = await storeController.WalletSettings(StoreId, "BTC");
        WalletSettingsViewModel walletSettings = (WalletSettingsViewModel)((ViewResult)response).Model;
        modify(walletSettings);
        storeController.UpdateWalletSettings(walletSettings).GetAwaiter().GetResult();
    }

    public async Task ModifyOnchainPaymentSettings(Action<WalletSettingsViewModel> modify)
    {
        UIStoresController storeController = GetController<UIStoresController>();
        IActionResult response = await storeController.WalletSettings(StoreId, "BTC");
        WalletSettingsViewModel walletSettings = (WalletSettingsViewModel)((ViewResult)response).Model;
        modify(walletSettings);
        storeController.UpdatePaymentSettings(walletSettings).GetAwaiter().GetResult();
    }

    public T GetController<T>(bool setImplicitStore = true) where T : Controller
    {
        T controller = parent.PayTester.GetController<T>(UserId, setImplicitStore ? StoreId : null, IsAdmin);
        return controller;
    }

    public async Task CreateStoreAsync()
    {
        if (UserId is null)
        {
            await RegisterAsync();
        }
        UIUserStoresController store = GetController<UIUserStoresController>();
        await store.CreateStore(new CreateStoreViewModel { Name = "Test Store" });
        StoreId = store.CreatedStoreId;
        parent.Stores.Add(StoreId);
    }

    public BTCPayNetwork SupportedNetwork { get; set; }

    public WalletId RegisterDerivationScheme(string crytoCode, ScriptPubKeyType segwit = ScriptPubKeyType.Legacy, bool importKeysToNBX = false)
    {
        return RegisterDerivationSchemeAsync(crytoCode, segwit, importKeysToNBX).GetAwaiter().GetResult();
    }

    public async Task<WalletId> RegisterDerivationSchemeAsync(string cryptoCode, ScriptPubKeyType segwit = ScriptPubKeyType.Legacy,
        bool importKeysToNBX = false, bool importsKeysToBitcoinCore = false)
    {
        if (StoreId is null)
        {
            await CreateStoreAsync();
        }

        SupportedNetwork = parent.NetworkProvider.GetNetwork<BTCPayNetwork>(cryptoCode);
        UIStoresController store = parent.PayTester.GetController<UIStoresController>(UserId, StoreId, true);

        var generateRequest = new WalletSetupRequest
        {
            ScriptPubKeyType = segwit,
            SavePrivateKeys = importKeysToNBX,
            ImportKeysToRPC = importsKeysToBitcoinCore
        };

        await store.GenerateWallet(StoreId, cryptoCode, WalletSetupMethod.HotWallet, generateRequest);
        Assert.NotNull(store.GenerateWalletResponse);
        GenerateWalletResponseV = store.GenerateWalletResponse;
        return new WalletId(StoreId, cryptoCode);
    }

    public GenerateWalletResponse GenerateWalletResponseV { get; set; }

    public DerivationStrategyBase DerivationScheme
    {
        get => GenerateWalletResponseV.DerivationScheme;
    }

    private async Task RegisterAsync(bool isAdmin = false)
    {
        UIAccountController account = parent.PayTester.GetController<UIAccountController>();
        RegisterDetails = new RegisterViewModel()
        {
            Email = Guid.NewGuid() + "@toto.com",
            ConfirmPassword = "Kitten0@",
            Password = "Kitten0@",
            IsAdmin = isAdmin
        };
        await account.Register(RegisterDetails);

        //this addresses an obscure issue where LockSubscription is unintentionally set to "true",
        //resulting in a large number of tests failing.  
        if (account.RegisteredUserId == null)
        {
            SettingsRepository settings = parent.PayTester.GetService<SettingsRepository>();
            PoliciesSettings policies = await settings.GetSettingAsync<PoliciesSettings>() ?? new PoliciesSettings();
            policies.LockSubscription = false;
            await account.Register(RegisterDetails);
        }

        UserId = account.RegisteredUserId;
        Email = RegisterDetails.Email;
        IsAdmin = account.RegisteredAdmin;
    }

    public RegisterViewModel RegisterDetails { get; set; }

    public Bitpay BitPay
    {
        get;
        set;
    }

    public string UserId
    {
        get;
        set;
    }

    public string Email
    {
        get;
        set;
    }

    public string StoreId
    {
        get;
        set;
    }

    public bool IsAdmin { get; internal set; }

    public void RegisterLightningNode(string cryptoCode, LightningConnectionType connectionType, bool isMerchant = true)
    {
        RegisterLightningNodeAsync(cryptoCode, connectionType, isMerchant).GetAwaiter().GetResult();
    }
    public Task RegisterLightningNodeAsync(string cryptoCode, bool isMerchant = true, string storeId = null)
    {
        return RegisterLightningNodeAsync(cryptoCode, null, isMerchant, storeId);
    }
    public async Task RegisterLightningNodeAsync(string cryptoCode, LightningConnectionType? connectionType, bool isMerchant = true, string storeId = null)
    {
        UIStoresController storeController = GetController<UIStoresController>();

        var connectionString = parent.GetLightningConnectionString(connectionType, isMerchant);
        LightningNodeType nodeType = connectionString == LightningSupportedPaymentMethod.InternalNode ? LightningNodeType.Internal : LightningNodeType.Custom;

        var vm = new LightningNodeViewModel { ConnectionString = connectionString, LightningNodeType = nodeType, SkipPortTest = true };
        await storeController.SetupLightningNode(storeId ?? StoreId,
            vm, "save", cryptoCode);
        if (storeController.ModelState.ErrorCount != 0)
        {
            Assert.False(true, storeController.ModelState.FirstOrDefault().Value.Errors[0].ErrorMessage);
        }
    }

    public async Task RegisterInternalLightningNodeAsync(string cryptoCode, string storeId = null)
    {
        UIStoresController storeController = GetController<UIStoresController>();
        var vm = new LightningNodeViewModel { ConnectionString = "", LightningNodeType = LightningNodeType.Internal, SkipPortTest = true };
        await storeController.SetupLightningNode(storeId ?? StoreId,
            vm, "save", cryptoCode);
        if (storeController.ModelState.ErrorCount != 0)
        {
            Assert.False(true, storeController.ModelState.FirstOrDefault().Value.Errors[0].ErrorMessage);
        }
    }

    public async Task<Coin> ReceiveUTXO(Money value, BTCPayNetwork network)
    {
        NBitcoin.RPC.RPCClient cashCow = parent.ExplorerNode;
        BTCPayWallet btcPayWallet = parent.PayTester.GetService<BTCPayWalletProvider>().GetWallet(network);
        BitcoinAddress address = (await btcPayWallet.ReserveAddressAsync(DerivationScheme)).Address;
        await parent.WaitForEvent<NewOnChainTransactionEvent>(async () =>
        {
            await cashCow.SendToAddressAsync(address, value);
        });
        int i = 0;
        while (i < 30)
        {
            Coin result = (await btcPayWallet.GetUnspentCoins(DerivationScheme))
                .FirstOrDefault(c => c.ScriptPubKey == address.ScriptPubKey)?.Coin;
            if (result != null)
            {
                return result;
            }

            await Task.Delay(1000);
            i++;
        }
        Assert.False(true);
        return null;
    }

    public async Task<BitcoinAddress> GetNewAddress(BTCPayNetwork network)
    {
        NBitcoin.RPC.RPCClient cashCow = parent.ExplorerNode;
        BTCPayWallet btcPayWallet = parent.PayTester.GetService<BTCPayWalletProvider>().GetWallet(network);
        BitcoinAddress address = (await btcPayWallet.ReserveAddressAsync(DerivationScheme)).Address;
        return address;
    }

    public async Task<PSBT> Sign(PSBT psbt)
    {
        BTCPayWallet btcPayWallet = parent.PayTester.GetService<BTCPayWalletProvider>()
            .GetWallet(psbt.Network.NetworkSet.CryptoCode);
        NBXplorer.ExplorerClient explorerClient = parent.PayTester.GetService<ExplorerClientProvider>()
            .GetExplorerClient(psbt.Network.NetworkSet.CryptoCode);
        psbt = (await explorerClient.UpdatePSBTAsync(new UpdatePSBTRequest()
        {
            DerivationScheme = DerivationScheme,
            PSBT = psbt
        })).PSBT;
        return psbt.SignAll(DerivationScheme, GenerateWalletResponseV.AccountHDKey,
            GenerateWalletResponseV.AccountKeyPath);
    }

    private Logging.ILog TestLogs => parent.TestLogs;
    public async Task<PSBT> SubmitPayjoin(Invoice invoice, PSBT psbt, string expectedError = null, bool senderError = false)
    {
        BitcoinUrlBuilder endpoint = GetPayjoinBitcoinUrl(invoice, psbt.Network);
        if (endpoint == null)
        {
            throw new InvalidOperationException("No payjoin endpoint for the invoice");
        }
        PayjoinClient pjClient = parent.PayTester.GetService<PayjoinClient>();
        StoreRepository storeRepository = parent.PayTester.GetService<StoreRepository>();
        Data.StoreData store = await storeRepository.FindStore(StoreId);
        DerivationSchemeSettings settings = store.GetSupportedPaymentMethods(parent.NetworkProvider).OfType<DerivationSchemeSettings>()
            .First();
        TestLogs.LogInformation($"Proposing {psbt.GetGlobalTransaction().GetHash()}");
        if (expectedError is null && !senderError)
        {
            PSBT proposed = await pjClient.RequestPayjoin(endpoint, new PayjoinWallet(settings), psbt, default);
            TestLogs.LogInformation($"Proposed payjoin is {proposed.GetGlobalTransaction().GetHash()}");
            Assert.NotNull(proposed);
            return proposed;
        }
        else
        {
            if (senderError)
            {
                await Assert.ThrowsAsync<PayjoinSenderException>(async () => await pjClient.RequestPayjoin(endpoint, new PayjoinWallet(settings), psbt, default));
            }
            else
            {
                PayjoinReceiverException ex = await Assert.ThrowsAsync<PayjoinReceiverException>(async () => await pjClient.RequestPayjoin(endpoint, new PayjoinWallet(settings), psbt, default));
                var split = expectedError.Split('|');
                Assert.Equal(split[0], ex.ErrorCode);
                if (split.Length > 1)
                {
                    Assert.Contains(split[1], ex.ReceiverMessage);
                }
            }
            return null;
        }
    }

    public async Task<Transaction> SubmitPayjoin(Invoice invoice, Transaction transaction, BTCPayNetwork network,
        string expectedError = null)
    {
        HttpResponseMessage response =
            await SubmitPayjoinCore(transaction.ToHex(), invoice, network.NBitcoinNetwork, expectedError);
        if (response == null)
        {
            return null;
        }

        var signed = Transaction.Parse(await response.Content.ReadAsStringAsync(), network.NBitcoinNetwork);
        return signed;
    }

    private async Task<HttpResponseMessage> SubmitPayjoinCore(string content, Invoice invoice, Network network,
        string expectedError)
    {
        BitcoinUrlBuilder bip21 = GetPayjoinBitcoinUrl(invoice, network);
        bip21.TryGetPayjoinEndpoint(out Uri endpoint);
        HttpResponseMessage response = await parent.PayTester.HttpClient.PostAsync(endpoint,
            new StringContent(content, Encoding.UTF8, "text/plain"));
        if (expectedError != null)
        {
            var split = expectedError.Split('|');
            Assert.False(response.IsSuccessStatusCode);
            var error = JObject.Parse(await response.Content.ReadAsStringAsync());
            if (split.Length > 0)
            {
                Assert.Equal(split[0], error["errorCode"].Value<string>());
            }

            if (split.Length > 1)
            {
                Assert.Contains(split[1], error["message"].Value<string>());
            }

            return null;
        }
        else
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = JObject.Parse(await response.Content.ReadAsStringAsync());
                Assert.True(false,
                    $"Error: {error["errorCode"].Value<string>()}: {error["message"].Value<string>()}");
            }
        }

        return response;
    }

    public static BitcoinUrlBuilder GetPayjoinBitcoinUrl(Invoice invoice, Network network)
    {
        var parsedBip21 = new BitcoinUrlBuilder(
            invoice.CryptoInfo.First(c => c.CryptoCode == network.NetworkSet.CryptoCode).PaymentUrls.BIP21,
            network);
        if (!parsedBip21.TryGetPayjoinEndpoint(out Uri endpoint))
        {
            return null;
        }

        return parsedBip21;
    }

    private class WebhookListener : IDisposable
    {
        private readonly Client.Models.StoreWebhookData _wh;
        private readonly FakeServer _server;
        private readonly List<WebhookInvoiceEvent> _webhookEvents;
        private readonly CancellationTokenSource _cts;
        public WebhookListener(Client.Models.StoreWebhookData wh, FakeServer server, List<WebhookInvoiceEvent> webhookEvents)
        {
            _wh = wh;
            _server = server;
            _webhookEvents = webhookEvents;
            _cts = new CancellationTokenSource();
            _ = Listen(_cts.Token);
        }

        private async Task Listen(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                Microsoft.AspNetCore.Http.HttpContext req = await _server.GetNextRequest(cancellation);
                var bytes = await req.Request.Body.ReadBytesAsync((int)req.Request.Headers.ContentLength);
                var callback = Encoding.UTF8.GetString(bytes);
                _webhookEvents.Add(JsonConvert.DeserializeObject<WebhookInvoiceEvent>(callback));
                req.Response.StatusCode = 200;
                _server.Done();
            }
        }
        public void Dispose()
        {
            _cts.Cancel();
            _server.Dispose();
        }
    }

    public List<WebhookInvoiceEvent> WebhookEvents { get; set; } = new List<WebhookInvoiceEvent>();
    public TEvent AssertHasWebhookEvent<TEvent>(WebhookEventType eventType, Action<TEvent> assert) where TEvent : class
    {
        int retry = 0;
retry:
        foreach (WebhookInvoiceEvent evt in WebhookEvents)
        {
            if (evt.Type == eventType)
            {
                TEvent typedEvt = evt.ReadAs<TEvent>();
                try
                {
                    assert(typedEvt);
                    return typedEvt;
                }
                catch (XunitException)
                {
                }
            }
        }
        if (retry < 3)
        {
            Thread.Sleep(1000);
            retry++;
            goto retry;
        }
        Assert.True(false, "No webhook event match the assertion");
        return null;
    }
    public async Task SetupWebhook()
    {
        FakeServer server = new FakeServer();
        await server.Start();
        BTCPayServerClient client = await CreateClient(Policies.CanModifyStoreWebhooks);
        Client.Models.StoreWebhookData wh = await client.CreateWebhook(StoreId, new CreateStoreWebhookRequest()
        {
            AutomaticRedelivery = false,
            Url = server.ServerUri.AbsoluteUri
        });

        parent.Resources.Add(new WebhookListener(wh, server, WebhookEvents));
    }

    public async Task PayInvoice(string invoiceId)
    {
        Invoice inv = await BitPay.GetInvoiceAsync(invoiceId);
        Network net = parent.ExplorerNode.Network;
        parent.ExplorerNode.SendToAddress(BitcoinAddress.Create(inv.BitcoinAddress, net), inv.BtcDue);
        await TestUtils.EventuallyAsync(async () =>
        {
            Invoice localInvoice = await BitPay.GetInvoiceAsync(invoiceId, Facade.Merchant);
            Assert.Equal("paid", localInvoice.Status);
        });
    }

    public async Task AddGuest(string userId)
    {
        StoreRepository repo = parent.PayTester.GetService<StoreRepository>();
        await repo.AddStoreUser(StoreId, userId, "Guest");
    }
    public async Task AddOwner(string userId)
    {
        StoreRepository repo = parent.PayTester.GetService<StoreRepository>();
        await repo.AddStoreUser(StoreId, userId, "Owner");
    }
}
