using DuelLedger.Core.Abstractions;

namespace DuelLedger.Core.Pipelines;

public class SnapshotAggregator
{
    public MatchSnapshot Current => _current;
    private MatchSnapshot _current = new(
        Format: 0,
        SelfClass: 0,
        OppClass: 0,
        Order: TurnOrder.Unknown,
        StartedAtUtc: null,
        EndedAtUtc: null,
        Result: MatchResult.Unknown);

    public MatchSnapshot Apply(DetectionResult detection)
    {
        if (detection.Format != 0)
            _current = _current with { Format = detection.Format };
        return _current;
    }
}
