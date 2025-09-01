using System;

using DuelLedger.Contracts;
namespace DuelLedger.UI.Models;

// 値は Publisher 側の列挙に合わせて固定（数値シリアライズ対策）
public enum PlayerClass
{
    エルフ = 1,
    ロイヤル = 2,
    ウィッチ = 3,
    ドラゴン = 4,
    ナイトメア = 5,
    ビショップ = 6,
    ネメシス = 7,
    Unknown = 0,
}

public enum TurnOrder { 先行 = 1, 後攻 = 2, Unknown = 0 }
public enum MatchResult { Win = 1, Lose = 2, Draw = 3, Unknown = 0 }

public enum MatchFormat
{
    Unknown = 0,
    Rank = 1,
    TwoPick = 2,
    GrandPrix = 3,
}

public sealed class MatchRecord
{
    public MatchFormat Format { get; init; }
    public PlayerClass SelfClass { get; init; }
    public PlayerClass OppClass { get; init; }
    public TurnOrder Order { get; init; }
    public MatchResult Result { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset EndedAt { get; init; }
    public bool IsInProgress { get; init; }

    public DateTimeOffset StartedAtLocal => StartedAt.ToLocalTime();
    public DateTimeOffset EndedAtLocal => EndedAt.ToLocalTime();
}