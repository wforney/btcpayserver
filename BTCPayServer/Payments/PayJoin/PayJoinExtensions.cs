using BTCPayServer.BIP78.Sender;
using BTCPayServer.Payments.PayJoin.Sender;
using BTCPayServer.Services;

namespace BTCPayServer.Payments.PayJoin;

public static class PayJoinExtensions
{
    public static void AddPayJoinServices(this IServiceCollection services)
    {
        services.AddSingleton<DelayedTransactionBroadcaster>();
        services.AddSingleton<IHostedService, HostedServices.DelayedTransactionBroadcasterHostedService>();
        services.AddSingleton<PayJoinRepository>();
        services.AddSingleton<IPayjoinServerCommunicator, PayjoinServerCommunicator>();
        services.AddSingleton<PayjoinClient>();
        services.AddTransient<Socks5HttpClientHandler>();
        services.AddHttpClient(PayjoinServerCommunicator.PayjoinOnionNamedClient)
            .ConfigurePrimaryHttpMessageHandler<Socks5HttpClientHandler>();
    }
}
