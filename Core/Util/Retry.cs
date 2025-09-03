namespace DuelLedger.Core.Util;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Exponential backoff retry helpers for transient I/O operations.
/// </summary>
public static class Retry
{
    public static void Run(Action action, int maxRetry = 3, int initialDelayMs = 30)
    {
        var delay = initialDelayMs;
        for (int i = 0;; i++)
        {
            try
            {
                action();
                return;
            }
            catch when (i < maxRetry - 1)
            {
                Thread.Sleep(delay);
                delay = Math.Min(delay * 2, 500);
            }
        }
    }

    public static async Task RunAsync(Func<Task> action, int maxRetry = 3, int initialDelayMs = 30)
    {
        var delay = initialDelayMs;
        for (int i = 0;; i++)
        {
            try
            {
                await action();
                return;
            }
            catch when (i < maxRetry - 1)
            {
                await Task.Delay(delay);
                delay = Math.Min(delay * 2, 500);
            }
        }
    }
}
