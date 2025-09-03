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
            if (_results.Count == 0)
            {
                result = default;
                return false;
            }
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
        var detector = new FakeDetector(new DetectionResult?[]{ new(1), new(1), new(1), null });
        var agg = new SnapshotAggregator();
        var pub = new FakePublisher();
        var pipe = new MatchPipeline<int>(source, new[]{detector}, agg, pub);

        pipe.Tick(); // 1
        pipe.Tick(); // 1
        pipe.Tick(); // 1 -> Format becomes 1
        pipe.Tick(); // null

        Assert.Equal(1, agg.Current.Format);
    }

    [Fact]
    public void UnknownDoesNotClearState()
    {
        var source = new FakeFrameSource();
        var detector = new FakeDetector(new DetectionResult?[]{ new(1), new(1), new(1), new(0), new(0) });
        var agg = new SnapshotAggregator();
        var pub = new FakePublisher();
        var pipe = new MatchPipeline<int>(source, new[]{detector}, agg, pub);

        pipe.Tick(); // 1
        pipe.Tick(); // 1
        pipe.Tick(); // 1 -> stabilizes at 1
        pipe.Tick(); // 0
        pipe.Tick(); // 0

        Assert.Equal(1, agg.Current.Format);
    }

    [Fact]
    public void RequiresThreeConsistentDetections()
    {
        var source = new FakeFrameSource();
        var detector = new FakeDetector(new DetectionResult?[]{ new(1), new(1), null, new(1) });
        var agg = new SnapshotAggregator();
        var pub = new FakePublisher();
        var pipe = new MatchPipeline<int>(source, new[]{detector}, agg, pub);

        pipe.Tick(); // 1
        Assert.Equal(0, agg.Current.Format);
        pipe.Tick(); // 1
        Assert.Equal(0, agg.Current.Format);
        pipe.Tick(); // null
        Assert.Equal(0, agg.Current.Format);
        pipe.Tick(); // 1

        Assert.Equal(1, agg.Current.Format);
    }
}
