using System;
using System.IO;
using System.Threading.Tasks;
using DuelLedger.Core.Abstractions;
using DuelLedger.Publishers;
using DuelLedger.UI.Services;

namespace DuelLedger.Tests;

public class MatchStateServiceTests
{
    [Fact]
    public async Task DetectsMatchLifecycleFromFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            using var svc = new MatchStateService(dir);
            var pub = new JsonStreamPublisher(dir);
            Assert.False(svc.IsInMatch);

            var start = DateTimeOffset.UtcNow;
            pub.PublishSnapshot(new MatchSnapshot(0, 0, 0, TurnOrder.Unknown, start, null, MatchResult.Unknown));
            await WaitForAsync(() => svc.IsInMatch);
            Assert.True(svc.IsInMatch);

            pub.PublishFinal(new MatchSummary(0, 0, 0, TurnOrder.Unknown, MatchResult.Win, start, start.AddMinutes(1)));
            await WaitForAsync(() => !svc.IsInMatch);
            Assert.False(svc.IsInMatch);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static async Task WaitForAsync(Func<bool> predicate)
    {
        for (int i = 0; i < 50; i++)
        {
            if (predicate()) return;
            await Task.Delay(20);
        }
        throw new TimeoutException("Condition not met in time");
    }
}
