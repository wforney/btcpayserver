using BTCPayServer.Abstractions.Contracts;

namespace Microsoft.AspNetCore.Hosting;

public static class WebHostExtensions
{
    public static async Task StartWithTasksAsync(this IWebHost webHost, CancellationToken cancellationToken = default)
    {
        // Load all tasks from DI
        System.Collections.Generic.IEnumerable<IStartupTask> startupTasks = webHost.Services.GetServices<IStartupTask>();

        // Execute all the tasks
        foreach (IStartupTask startupTask in startupTasks)
        {
            await startupTask.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        // Start the tasks as normal
        await webHost.StartAsync(cancellationToken).ConfigureAwait(false);
    }
}
