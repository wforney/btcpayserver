using System.Reflection;
using System.Text;
using BundlerMinifier.TagHelpers;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Hosting;

public class ResourceBundleProvider : IBundleProvider
{
    private readonly BundleProvider _InnerProvider;
    private readonly Lazy<Dictionary<string, Bundle>> _BundlesByName;
    public ResourceBundleProvider(IWebHostEnvironment hosting, BundleOptions options)
    {
        if (options.UseBundles)
        {
            _BundlesByName = new Lazy<Dictionary<string, Bundle>>(() =>
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BTCPayServer.bundleconfig.json");
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = reader.ReadToEnd();
                return JArray.Parse(content).OfType<JObject>()
                       .Select(jobj => new Bundle()
                       {
                           Name = jobj.Property("name", StringComparison.OrdinalIgnoreCase)?.Value.Value<string>() ?? jobj.Property("outputFileName", StringComparison.OrdinalIgnoreCase).Value.Value<string>(),
                           OutputFileUrl = Path.Combine(hosting.ContentRootPath, jobj.Property("outputFileName", StringComparison.OrdinalIgnoreCase).Value.Value<string>())
                       }).ToDictionary(o => o.Name, o => o);
            }, true);
        }
        else
        {
            _InnerProvider = new BundleProvider(hosting);
        }
    }
    public Bundle GetBundle(string name)
    {
        if (_InnerProvider != null)
        {
            return _InnerProvider.GetBundle(name);
        }

        _BundlesByName.Value.TryGetValue(name, out Bundle bundle);
        return bundle;
    }
}
