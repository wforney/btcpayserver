using System.Globalization;
using System.Text;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Data;
using BTCPayServer.Filters;
using BTCPayServer.Models;
using BTCPayServer.Rating;
using BTCPayServer.Security;
using BTCPayServer.Security.Bitpay;
using BTCPayServer.Services.Rates;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BTCPayServer.Controllers;

[EnableCors(CorsPolicies.All)]
[Authorize(Policy = ServerPolicies.CanGetRates.Key, AuthenticationSchemes = AuthenticationSchemes.Bitpay)]
public class BitpayRateController : Controller
{
    public StoreData CurrentStore
    {
        get
        {
            return HttpContext.GetStoreData();
        }
    }

    private readonly RateFetcher _RateProviderFactory;
    private readonly BTCPayNetworkProvider _NetworkProvider;
    private readonly CurrencyNameTable _CurrencyNameTable;
    private readonly StoreRepository _StoreRepo;

    public TokenRepository TokenRepository { get; }

    public BitpayRateController(
        RateFetcher rateProviderFactory,
        BTCPayNetworkProvider networkProvider,
        TokenRepository tokenRepository,
        StoreRepository storeRepo,
        CurrencyNameTable currencyNameTable)
    {
        _RateProviderFactory = rateProviderFactory ?? throw new ArgumentNullException(nameof(rateProviderFactory));
        _NetworkProvider = networkProvider;
        TokenRepository = tokenRepository;
        _StoreRepo = storeRepo;
        _CurrencyNameTable = currencyNameTable ?? throw new ArgumentNullException(nameof(currencyNameTable));
    }

    [Route("rates/{baseCurrency}")]
    [HttpGet]
    [BitpayAPIConstraint]
    public async Task<IActionResult> GetBaseCurrencyRates(string baseCurrency, CancellationToken cancellationToken)
    {
        IEnumerable<Payments.ISupportedPaymentMethod> supportedMethods = CurrentStore.GetSupportedPaymentMethods(_NetworkProvider);

        IEnumerable<string> currencyCodes = supportedMethods.Where(method => !string.IsNullOrEmpty(method.PaymentId.CryptoCode))
            .Select(method => method.PaymentId.CryptoCode).Distinct();

        var currencypairs = BuildCurrencyPairs(currencyCodes, baseCurrency);

        IActionResult result = await GetRates2(currencypairs, null, cancellationToken);
        var rates = (result as JsonResult)?.Value as Rate[];
        if (rates == null)
        {
            return result;
        }

        return Json(new DataWrapper<Rate[]>(rates));
    }

    [Route("rates/{baseCurrency}/{currency}")]
    [HttpGet]
    [BitpayAPIConstraint]
    public async Task<IActionResult> GetCurrencyPairRate(string baseCurrency, string currency, CancellationToken cancellationToken)
    {
        IActionResult result = await GetRates2($"{baseCurrency}_{currency}", null, cancellationToken);
        var rates = (result as JsonResult)?.Value as Rate[];
        if (rates == null)
        {
            return result;
        }

        return Json(new DataWrapper<Rate>(rates.First()));
    }

    [Route("rates")]
    [HttpGet]
    [BitpayAPIConstraint]
    public async Task<IActionResult> GetRates(string currencyPairs, string storeId = null, CancellationToken cancellationToken = default)
    {
        IActionResult result = await GetRates2(currencyPairs, storeId, cancellationToken);
        var rates = (result as JsonResult)?.Value as Rate[];
        if (rates == null)
        {
            return result;
        }

        return Json(new DataWrapper<Rate[]>(rates));
    }

    [Route("api/rates")]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetRates2(string currencyPairs, string storeId, CancellationToken cancellationToken)
    {
        StoreData store = CurrentStore ?? await _StoreRepo.FindStore(storeId);
        if (store == null)
        {
            JsonResult err = Json(new BitpayErrorsModel() { Error = "Store not found" });
            err.StatusCode = 404;
            return err;
        }
        if (currencyPairs == null)
        {
            currencyPairs = store.GetStoreBlob().GetDefaultCurrencyPairString();
            if (string.IsNullOrEmpty(currencyPairs))
            {
                JsonResult result = Json(new BitpayErrorsModel() { Error = "You need to setup the default currency pairs in 'Store Settings / Rates' or specify 'currencyPairs' query parameter (eg. BTC_USD,LTC_CAD)." });
                result.StatusCode = 400;
                return result;
            }
        }


        RateRules rules = store.GetStoreBlob().GetRateRules(_NetworkProvider);

        HashSet<CurrencyPair> pairs = new HashSet<CurrencyPair>();
        foreach (var currency in currencyPairs.Split(','))
        {
            if (!CurrencyPair.TryParse(currency, out CurrencyPair pair))
            {
                JsonResult result = Json(new BitpayErrorsModel() { Error = $"Currency pair {currency} uncorrectly formatted" });
                result.StatusCode = 400;
                return result;
            }
            pairs.Add(pair);
        }

        Dictionary<CurrencyPair, Task<RateResult>> fetching = _RateProviderFactory.FetchRates(pairs, rules, cancellationToken);
        await Task.WhenAll(fetching.Select(f => f.Value).ToArray());
        return Json(pairs
                        .Select(r => (Pair: r, Value: fetching[r].GetAwaiter().GetResult().BidAsk?.Bid))
                        .Where(r => r.Value.HasValue)
                        .Select(r =>
                        new Rate()
                        {
                            CryptoCode = r.Pair.Left,
                            Code = r.Pair.Right,
                            CurrencyPair = r.Pair.ToString(),
                            Name = _CurrencyNameTable.GetCurrencyData(r.Pair.Right, true).Name,
                            Value = r.Value.Value
                        }).Where(n => n.Name != null).ToArray());
    }

    private static string BuildCurrencyPairs(IEnumerable<string> currencyCodes, string baseCrypto)
    {
        StringBuilder currencyPairsBuilder = new StringBuilder();
        bool first = true;
        foreach (var currencyCode in currencyCodes)
        {
            if (!first)
            {
                currencyPairsBuilder.Append(',');
            }

            first = false;
            currencyPairsBuilder.Append(CultureInfo.InvariantCulture, $"{baseCrypto}_{currencyCode}");
        }
        return currencyPairsBuilder.ToString();
    }

    public class Rate
    {

        [JsonProperty(PropertyName = "name")]
        public string Name
        {
            get;
            set;
        }
        [JsonProperty(PropertyName = "cryptoCode")]
        public string CryptoCode
        {
            get;
            set;
        }

        [JsonProperty(PropertyName = "currencyPair")]
        public string CurrencyPair
        {
            get;
            set;
        }

        [JsonProperty(PropertyName = "code")]
        public string Code
        {
            get;
            set;
        }
        [JsonProperty(PropertyName = "rate")]
        public decimal Value
        {
            get;
            set;
        }
    }
}
