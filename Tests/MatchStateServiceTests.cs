using System.Text.Json;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.Tests;

public class MatchStateServiceTests
{
    [Fact]
    public async Task DetectsMatchStartWhenSnapshotWritten()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            using var svc = new MatchStateService(dir);
            var tcs = new TaskCompletionSource<bool>();
            svc.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MatchStateService.IsInMatch) && svc.IsInMatch)
                    tcs.TrySetResult(true);
            };

            var dto = new MatchSnapshotDto
            {
                SelfClass = 1,
                StartedAt = DateTimeOffset.UtcNow,
                EndedAt = null
            };
            var tmp = Path.Combine(dir, "current.json.tmp");
            var path = Path.Combine(dir, "current.json");
            await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(dto));
            File.Move(tmp, path, true);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Assert.True(completed == tcs.Task && await tcs.Task);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}


