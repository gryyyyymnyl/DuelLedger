namespace DuelLedger.Core;

using System;

using DuelLedger.Core.Abstractions;
public sealed class MatchAggregator
{
    private readonly object _lock = new();
    private readonly IMatchPublisher _publisher;
    private int _formatId = 0; // Unknown
    private int _selfId = 0; // Unknown
    private int _oppId = 0; // Unknown
    private TurnOrder _order = TurnOrder.Unknown;
    private DateTimeOffset? _startAtUtc = null;
    private DateTimeOffset? _endAtUtc = null;
    private MatchResult _result = MatchResult.Unknown;

    public MatchAggregator(IMatchPublisher publisher) => _publisher = publisher;

    public void OnMatchStarted(DateTimeOffset nowUtc)
    {
        lock (_lock)
        {
            _selfId = 0;
            _oppId = 0;
            _order = TurnOrder.Unknown;
            _result = MatchResult.Unknown;
            _startAtUtc = nowUtc;
            _endAtUtc = null;
            _publisher.PublishSnapshot(Snapshot());
        }
    }

    public void OnTurnOrderDetected(TurnOrder order)
    {
        lock (_lock)
        {
            _order = order;
            _publisher.PublishSnapshot(Snapshot());
        }
    }

    // ゲーム側から検知した試合形式（例: "Rotation", "Unlimited" など）を渡す
    public void OnFormatDetected(string formatLabel)
    {
        if (string.IsNullOrWhiteSpace(formatLabel)) return;
        lock (_lock)
        {
            _formatId = MatchContracts.ToFormatId(formatLabel.Trim());
            _publisher.PublishSnapshot(Snapshot());
        }
    }

    public void OnClassesDetected(string selfLabel, string oppLabel)
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(selfLabel) && selfLabel != "Unknown")
                _selfId = MatchContracts.ToClassId(selfLabel);
            if (!string.IsNullOrWhiteSpace(oppLabel) && oppLabel != "Unknown")
                _oppId = MatchContracts.ToClassId(oppLabel);
            _publisher.PublishSnapshot(Snapshot());
        }
    }

    public void OnMatchEnded(MatchResult result, DateTimeOffset nowUtc)
    {
        MatchSummary summary;
        lock (_lock)
        {
            _result = result;
            _endAtUtc = nowUtc;

            summary = new MatchSummary(
                            _formatId,
                            _selfId,
                            _oppId,
                            _order,
                            _result,
                            _startAtUtc ?? nowUtc,
                            nowUtc
                        );
        }
        _publisher.PublishFinal(summary);
    }

    private MatchSnapshot Snapshot() => new(
        _formatId,
        _selfId,
        _oppId,
        _order,
        _startAtUtc,
        _endAtUtc,
        _result
    );
}
