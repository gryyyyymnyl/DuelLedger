using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core.Pipelines;
using Xunit;

public class MatchPipelineTests
{
    private sealed class FakeFrameSource : IFrameSource<int>
    {
        public bool TryGetFrame(out int frame)
        {
            frame = 0;
            return true;
        }
    }

    private sealed class FakeDetector : IDetector<int>
    {
        private readonly Queue<DetectionResult?> _results;
        public FakeDetector(IEnumerable<DetectionResult?> results) => _results = new(results);
        public bool TryDetect(int frame, out DetectionResult result)
        {
            var item = _results.Dequeue();
            if (item.HasValue)
            {
                result = item.Value;
                return true;
            }
            result = default;
            return false;
        }
    }

    private sealed class FakePublisher : ISnapshotPublisher
    {
        public readonly List<MatchSnapshot> Published = new();
        public Task PublishAsync(MatchSnapshot snapshot, CancellationToken ct = default)
        {
            Published.Add(snapshot);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void DetectionFailureRetainsFormat()
    {
        var source = new FakeFrameSource();
        var detector = new FakeDetector(new DetectionResult?[]{ new(1), null });
        var agg = new SnapshotAggregator();
        var pub = new FakePublisher();
        var pipe = new MatchPipeline<int>(source, new[]{detector}, agg, pub);

        pipe.Tick();
        pipe.Tick();

        Assert.Equal(1, agg.Current.Format);
    }

    [Fact]
    public void UnknownDoesNotClearState()
    {
        var source = new FakeFrameSource();
        var detector = new FakeDetector(new DetectionResult?[]{ new(1), new(0), new(0) });
        var agg = new SnapshotAggregator();
        var pub = new FakePublisher();
        var pipe = new MatchPipeline<int>(source, new[]{detector}, agg, pub);

        pipe.Tick();
        pipe.Tick();
        pipe.Tick();

        Assert.All(pub.Published, s => Assert.Equal(1, s.Format));
    }
}
