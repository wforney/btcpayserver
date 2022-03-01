using BTCPayServer.Client;

namespace BTCPayServer.Abstractions.Contracts;

public interface IBTCPayServerClientFactory
{
    Task<BTCPayServerClient> Create(string userId, params string[] storeIds);
}
