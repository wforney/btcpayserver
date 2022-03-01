using System.Runtime.CompilerServices;

namespace BTCPayServer;

public struct SynchronizationContextRemover : INotifyCompletion
{
    public bool IsCompleted => SynchronizationContext.Current == null;

    public void OnCompleted(Action continuation)
    {
        SynchronizationContext prev = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            continuation();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }

    public SynchronizationContextRemover GetAwaiter()
    {
        return this;
    }

    public void GetResult()
    {
    }
}
