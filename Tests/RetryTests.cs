using System.IO;
using DuelLedger.Core.Util;

namespace DuelLedger.Tests;

public class RetryTests
{
    [Fact]
    public void RetriesSpecifiedTimes()
    {
        int attempts = 0;
        Assert.Throws<IOException>(() => Retry.Run(() =>
        {
            attempts++;
            throw new IOException();
        }, maxRetry: 3, initialDelayMs: 1));
        Assert.Equal(3, attempts);
    }

    [Fact]
    public void EventuallySucceeds()
    {
        int attempts = 0;
        Retry.Run(() =>
        {
            if (attempts++ < 2)
                throw new IOException();
        }, maxRetry: 5, initialDelayMs: 1);
        Assert.Equal(3, attempts);
    }
}
