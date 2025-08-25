using System;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.ViewModels;

public sealed class HistoryTileVm : NotifyBase
{
    public string Id { get; }
    public double Width { get; init; }
    public double Height { get; init; }

    private double _left;
    public double Left { get => _left; set => Set(ref _left, value); }

    private double _top;
    public double Top { get => _top; set => Set(ref _top, value); }

    private bool _isEntering;
    public bool IsEntering { get => _isEntering; set => Set(ref _isEntering, value); }

    public long OrderKey { get; init; }

    public MatchRecord Record { get; }

    public HistoryTileVm(MatchRecord record, double width = 110, double height = 110)
    {
        Record = record;
        Id = Guid.NewGuid().ToString();
        OrderKey = record.EndedAt.ToUnixTimeMilliseconds();
        Width = width;
        Height = height;
    }

    // Proxy properties for binding
    public DateTimeOffset EndedAtLocal => Record.EndedAtLocal;
    public PlayerClass SelfClass => Record.SelfClass;
    public PlayerClass OppClass => Record.OppClass;
    public TurnOrder Order => Record.Order;
    public MatchResult Result => Record.Result;
}
