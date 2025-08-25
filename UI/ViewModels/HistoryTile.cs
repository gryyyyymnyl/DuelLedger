using System;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.ViewModels;

public sealed class HistoryTile : NotifyBase
{
    public HistoryTile(MatchRecord record)
    {
        Record = record;
    }

    public MatchRecord Record { get; }

    public MatchFormat Format => Record.Format;
    public PlayerClass SelfClass => Record.SelfClass;
    public PlayerClass OppClass => Record.OppClass;
    public TurnOrder Order => Record.Order;
    public MatchResult Result => Record.Result;
    public DateTimeOffset StartedAt => Record.StartedAt;
    public DateTimeOffset EndedAt => Record.EndedAt;
    public DateTimeOffset StartedAtLocal => Record.StartedAtLocal;
    public DateTimeOffset EndedAtLocal => Record.EndedAtLocal;

    private double _left;
    public double Left
    {
        get => _left;
        set => Set(ref _left, value);
    }

    private double _top;
    public double Top
    {
        get => _top;
        set => Set(ref _top, value);
    }
}
