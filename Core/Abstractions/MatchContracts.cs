namespace DuelLedger.Core.Abstractions;

public enum TurnOrder { Unknown, 先行, 後攻  }
public enum MatchResult { Unknown, Win, Lose, Draw  }

// Format / Class は全て ID で保持（0 = Unknown）
public sealed record MatchSummary(
    int Format,
    int SelfClass,
    int OppClass,
    TurnOrder Order,
    MatchResult Result,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt
);

public sealed record MatchSnapshot(
    int Format,
    int SelfClass,
    int OppClass,
    TurnOrder Order,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    MatchResult Result // 進行中は Unknown
);

public static class MatchContracts
{
    private static Func<string, int>? _classIdMapper;
    private static Func<string, int>? _formatIdMapper;

    public static void SetClassIdMapper(Func<string, int> mapper)
        => _classIdMapper = mapper;

    public static void SetFormatIdMapper(Func<string, int> mapper)
        => _formatIdMapper = mapper;

    public static int ToClassId(string label)
        => _classIdMapper != null ? _classIdMapper(label) : 0;

    public static int ToFormatId(string label)
        => _formatIdMapper != null ? _formatIdMapper(label) : 0;
}
