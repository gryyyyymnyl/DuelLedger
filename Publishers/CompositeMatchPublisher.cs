using System.Collections.Generic;
using DuelLedger.Contracts;
using DuelLedger.Core;

namespace DuelLedger.Publishers;

/// <summary>
/// Forwards match events to multiple underlying publishers.
/// </summary>
public sealed class CompositeMatchPublisher : IMatchPublisher
{
    private readonly IReadOnlyList<IMatchPublisher> _publishers;

    public CompositeMatchPublisher(params IMatchPublisher[] publishers)
        => _publishers = publishers;

    public void PublishSnapshot(MatchSnapshot snapshot)
    {
        foreach (var p in _publishers)
            p.PublishSnapshot(snapshot);
    }

    public void PublishFinal(MatchSummary summary)
    {
        foreach (var p in _publishers)
            p.PublishFinal(summary);
    }
}
