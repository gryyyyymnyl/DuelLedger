using DuelLedger.Contracts;
using DuelLedger.Publishers;

namespace DuelLedger.Tests;

public class JsonStreamPublisherTests
{
    [Fact]
    public void PublishSnapshot_OverwritesOpenFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var publisher = new JsonStreamPublisher(dir);
            var path = Path.Combine(dir, "current.json");

            // seed an initial file and keep it open with delete sharing
            File.WriteAllText(path, "{}");
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            var snap = new MatchSnapshot(
                0, 0, 0, TurnOrder.Unknown,
                DateTimeOffset.UtcNow, null, MatchResult.Unknown);

            publisher.PublishSnapshot(snap);

            Assert.True(File.Exists(path));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

