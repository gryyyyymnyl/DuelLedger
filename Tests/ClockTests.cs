using System;
using System.Threading.Tasks;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core.Util;
using Xunit;

namespace DuelLedger.Tests;

public class ClockTests
{
    [Fact]
    public async Task SystemClock_Monotonic()
    {
        IClock clock = SystemClock.Instance;
        var t1 = clock.Now;
        await Task.Delay(5);
        var t2 = clock.Now;
        Assert.True(t2 >= t1);
    }
}
