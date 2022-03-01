using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace BTCPayServer.Tests;

public class FakeServer : IDisposable
{
    private IWebHost webHost;
    private readonly SemaphoreSlim semaphore;
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    public FakeServer()
    {
        _channel = Channel.CreateUnbounded<HttpContext>();
        semaphore = new SemaphoreSlim(0);
    }

    private readonly Channel<HttpContext> _channel;
    public async Task Start()
    {
        webHost = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0")
                .Configure(appBuilder =>
                {
                    appBuilder.Run(async ctx =>
                    {
                        await _channel.Writer.WriteAsync(ctx);
                        await semaphore.WaitAsync(cts.Token);
                    });
                })
                .Build();
        await webHost.StartAsync();
        var port = new Uri(webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First(), UriKind.Absolute)
            .Port;
        ServerUri = new Uri($"http://127.0.0.1:{port}/");
    }

    public Uri ServerUri { get; set; }

    public void Done()
    {
        semaphore.Release();
    }

    public async Task Stop()
    {
        await webHost.StopAsync();
    }
    public void Dispose()
    {
        cts.Dispose();
        webHost?.Dispose();
        semaphore.Dispose();
    }

    public async Task<HttpContext> GetNextRequest(CancellationToken cancellationToken = default)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
