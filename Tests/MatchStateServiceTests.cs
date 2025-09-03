using DuelLedger.Core.Abstractions;
using DuelLedger.UI.Services;

namespace DuelLedger.Tests;

public class MatchStateServiceTests
{
    [Fact]
    public void TracksMatchLifecycle()
    {
        using var svc = new MatchStateService(Path.GetTempPath());
        Assert.False(svc.IsInMatch);

        var start = DateTimeOffset.UtcNow;
        svc.PublishSnapshot(new MatchSnapshot(0, 0, 0, TurnOrder.Unknown, start, null, MatchResult.Unknown));
        Assert.True(svc.IsInMatch);

        svc.PublishFinal(new MatchSummary(0, 0, 0, TurnOrder.Unknown, MatchResult.Win, start, start.AddMinutes(1)));
        Assert.False(svc.IsInMatch);
    }
}
