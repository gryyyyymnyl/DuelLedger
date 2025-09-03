using System.Collections.Generic;
using System.Threading.Channels;
using DuelLedger.Core.Abstractions;

namespace DuelLedger.Core.Pipelines;

public class MatchPipeline<TFrame>
{
    private readonly IFrameSource<TFrame> _source;
    private readonly IEnumerable<IDetector<TFrame>> _detectors;
    private readonly SnapshotAggregator _aggregator;
    private readonly ISnapshotPublisher _publisher;
    private readonly Channel<MatchSnapshot> _channel = Channel.CreateUnbounded<MatchSnapshot>();
    private readonly CancellationTokenSource _cts = new();

    public MatchPipeline(
        IFrameSource<TFrame> source,
        IEnumerable<IDetector<TFrame>> detectors,
        SnapshotAggregator aggregator,
        ISnapshotPublisher publisher)
    {
        _source = source;
        _detectors = detectors;
        _aggregator = aggregator;
        _publisher = publisher;
        _ = Task.Run(PublishLoop);
    }

    private async Task PublishLoop()
    {
        await foreach (var snapshot in _channel.Reader.ReadAllAsync(_cts.Token))
            await _publisher.PublishAsync(snapshot, _cts.Token);
    }

    public void Tick()
    {
        if (!_source.TryGetFrame(out var frame))
            return;
        foreach (var detector in _detectors)
        {
            if (detector.TryDetect(frame, out var result))
            {
                var snapshot = _aggregator.Apply(result);
                _channel.Writer.TryWrite(snapshot);
            }
        }
    }

    public void Stop()
    {
        _channel.Writer.Complete();
        _cts.Cancel();
    }
}
