namespace DuelLedger.Core;

using DuelLedger.Contracts;

/// <summary>
/// No-op implementation of <see cref="IMatchPublisher"/> used when no publisher is provided.
/// </summary>
public sealed class NullPublisher : IMatchPublisher
{
    public void PublishSnapshot(MatchSnapshot snapshot) { }
    public void PublishFinal(MatchSummary summary) { }
}