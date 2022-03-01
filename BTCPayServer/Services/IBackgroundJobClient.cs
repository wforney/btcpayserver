namespace BTCPayServer.Services;

public interface IBackgroundJobClient
{
    void Schedule(Func<CancellationToken, Task> act, TimeSpan scheduledIn);
}
