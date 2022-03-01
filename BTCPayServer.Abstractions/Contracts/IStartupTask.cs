namespace BTCPayServer.Abstractions.Contracts;

public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
