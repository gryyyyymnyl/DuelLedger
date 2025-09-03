using System.Collections.Generic;
using System.Linq;
using DuelLedger.Core.Abstractions;

namespace DuelLedger.Core.Pipelines;

public class SnapshotAggregator
{
    private readonly Queue<int> _formats = new();
    private const int BufferSize = 10;

    public MatchSnapshot Current => _current;
    private MatchSnapshot _current = new(
        Format: 0,
        SelfClass: 0,
        OppClass: 0,
        Order: TurnOrder.Unknown,
        StartedAt: null,
        EndedAt: null,
        Result: MatchResult.Unknown);

    public MatchSnapshot Apply(DetectionResult detection)
    {
        _formats.Enqueue(detection.Format);
        if (_formats.Count > BufferSize)
            _formats.Dequeue();

        var majority = _formats
            .Where(f => f > 0)
            .GroupBy(f => f)
            .Select(g => new { Format = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefault();

        if (majority is { Count: >= 3 })
            _current = _current with { Format = majority.Format };

        return _current;
    }
}
