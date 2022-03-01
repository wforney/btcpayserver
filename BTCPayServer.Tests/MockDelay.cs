using System.Globalization;

namespace BTCPayServer.Tests;

public class MockDelay : IDelay
{
    private class WaitObj
    {
        public DateTimeOffset Expiration;
        public TaskCompletionSource<bool> CTS;
    }

    private readonly List<WaitObj> waits = new List<WaitObj>();
    private DateTimeOffset _Now = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public async Task Wait(TimeSpan delay, CancellationToken cancellation)
    {
        WaitObj w = new WaitObj
        {
            Expiration = _Now + delay,
            CTS = new TaskCompletionSource<bool>()
        };
        using (cancellation.Register(() =>
         {
             w.CTS.TrySetCanceled();
         }))
        {
            lock (waits)
            {
                waits.Add(w);
            }
            await w.CTS.Task;
        }
    }

    public async Task Advance(TimeSpan time)
    {
        _Now += time;
        List<WaitObj> overdue = new List<WaitObj>();
        lock (waits)
        {
            foreach (WaitObj wait in waits.ToArray())
            {
                if (_Now >= wait.Expiration)
                {
                    overdue.Add(wait);
                    waits.Remove(wait);
                }
            }
        }
        foreach (WaitObj o in overdue)
        {
            o.CTS.TrySetResult(true);
        }

        try
        {
            await Task.WhenAll(overdue.Select(o => o.CTS.Task).ToArray());
        }
        catch { }
    }
    public override string ToString()
    {
        return _Now.Millisecond.ToString(CultureInfo.InvariantCulture);
    }
}
