using System.Globalization;
using System.Reflection;
using System.Text;
using BTCPayServer.Rating;
using NBitcoin;
using Newtonsoft.Json;

namespace BTCPayServer.Services.Rates;

public class CurrencyData
{
    public string Name { get; set; }
    public string Code { get; set; }
    public int Divisibility { get; set; }
    public string Symbol { get; set; }
    public bool Crypto { get; set; }
}
public class CurrencyNameTable
{
    public static CurrencyNameTable Instance = new CurrencyNameTable();
    public CurrencyNameTable()
    {
        _Currencies = LoadCurrency().ToDictionary(k => k.Code);
    }

    private static readonly Dictionary<string, IFormatProvider> _CurrencyProviders = new Dictionary<string, IFormatProvider>();
    public string FormatCurrency(string price, string currency)
    {
        return FormatCurrency(decimal.Parse(price, CultureInfo.InvariantCulture), currency);
    }
    public string FormatCurrency(decimal price, string currency)
    {
        return price.ToString("C", GetCurrencyProvider(currency));
    }

    public NumberFormatInfo GetNumberFormatInfo(string currency, bool useFallback)
    {
        IFormatProvider data = GetCurrencyProvider(currency);
        if (data is NumberFormatInfo nfi)
        {
            return nfi;
        }

        if (data is CultureInfo ci)
        {
            return ci.NumberFormat;
        }

        if (!useFallback)
        {
            return null;
        }

        return CreateFallbackCurrencyFormatInfo(currency);
    }

    private NumberFormatInfo CreateFallbackCurrencyFormatInfo(string currency)
    {
        NumberFormatInfo usd = GetNumberFormatInfo("USD", false);
        var currencyInfo = (NumberFormatInfo)usd.Clone();
        currencyInfo.CurrencySymbol = currency;
        return currencyInfo;
    }
    public NumberFormatInfo GetNumberFormatInfo(string currency)
    {
        IFormatProvider curr = GetCurrencyProvider(currency);
        if (curr is CultureInfo cu)
        {
            return cu.NumberFormat;
        }

        if (curr is NumberFormatInfo ni)
        {
            return ni;
        }

        return null;
    }
    public IFormatProvider GetCurrencyProvider(string currency)
    {
        lock (_CurrencyProviders)
        {
            if (_CurrencyProviders.Count == 0)
            {
                foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => !c.IsNeutralCulture))
                {
                    try
                    {
                        _CurrencyProviders.TryAdd(new RegionInfo(culture.LCID).ISOCurrencySymbol, culture);
                    }
                    catch { }
                }

                foreach (KeyValuePair<string, CurrencyData> curr in _Currencies.Where(pair => pair.Value.Crypto))
                {
                    AddCurrency(_CurrencyProviders, curr.Key, curr.Value.Divisibility, curr.Value.Symbol ?? curr.Value.Code);
                }
            }
            return _CurrencyProviders.TryGet(currency.ToUpperInvariant());
        }
    }

    private void AddCurrency(Dictionary<string, IFormatProvider> currencyProviders, string code, int divisibility, string symbol)
    {
        var culture = new CultureInfo("en-US");
        var number = new NumberFormatInfo
        {
            CurrencyDecimalDigits = divisibility,
            CurrencySymbol = symbol,
            CurrencyDecimalSeparator = culture.NumberFormat.CurrencyDecimalSeparator,
            CurrencyGroupSeparator = culture.NumberFormat.CurrencyGroupSeparator,
            CurrencyGroupSizes = culture.NumberFormat.CurrencyGroupSizes,
            CurrencyNegativePattern = 8,
            CurrencyPositivePattern = 3,
            NegativeSign = culture.NumberFormat.NegativeSign
        };
        currencyProviders.TryAdd(code, number);
    }

    /// <summary>
    /// Format a currency like "0.004 $ (USD)", round to significant divisibility
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="currency">Currency code</param>
    /// <returns></returns>
    public string DisplayFormatCurrency(decimal value, string currency)
    {
        NumberFormatInfo provider = GetNumberFormatInfo(currency, true);
        CurrencyData currencyData = GetCurrencyData(currency, true);
        var divisibility = currencyData.Divisibility;
        value = value.RoundToSignificant(ref divisibility);
        if (divisibility != provider.CurrencyDecimalDigits)
        {
            provider = (NumberFormatInfo)provider.Clone();
            provider.CurrencyDecimalDigits = divisibility;
        }

        if (currencyData.Crypto)
        {
            return value.ToString("C", provider);
        }
        else
        {
            return value.ToString("C", provider) + $" ({currency})";
        }
    }

    private readonly Dictionary<string, CurrencyData> _Currencies;

    private static CurrencyData[] LoadCurrency()
    {
        Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BTCPayServer.Rating.Currencies.json");
        string content = null;
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            content = reader.ReadToEnd();
        }

        CurrencyData[] currencies = JsonConvert.DeserializeObject<CurrencyData[]>(content);
        return currencies;
    }

    public IEnumerable<CurrencyData> Currencies => _Currencies.Values;

    public CurrencyData GetCurrencyData(string currency, bool useFallback)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (!_Currencies.TryGetValue(currency.ToUpperInvariant(), out CurrencyData result))
        {
            if (useFallback)
            {
                CurrencyData usd = GetCurrencyData("USD", false);
                result = new CurrencyData()
                {
                    Code = currency,
                    Crypto = true,
                    Name = currency,
                    Divisibility = usd.Divisibility
                };
            }
        }
        return result;
    }

}
