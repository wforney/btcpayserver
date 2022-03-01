using System.Net;
using BTCPayServer.Configuration;

namespace BTCPayServer.Services;

public class Socks5HttpClientHandler : HttpClientHandler
{
    public Socks5HttpClientHandler(BTCPayServerOptions opts)
    {
        if (opts.SocksEndpoint is IPEndPoint endpoint)
        {
            Proxy = new WebProxy($"socks5://{endpoint.Address}:{endpoint.Port}");
        }
        else if (opts.SocksEndpoint is DnsEndPoint endpoint2)
        {
            Proxy = new WebProxy($"socks5://{endpoint2.Host}:{endpoint2.Port}");
        }
    }
}
