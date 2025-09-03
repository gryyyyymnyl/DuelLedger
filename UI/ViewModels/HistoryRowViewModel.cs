using System;
using Avalonia;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.ViewModels;

public sealed class HistoryRowViewModel : NotifyBase, IDisposable
{
    public MatchRecord Record { get; }
    public PlayerClass SelfClass => Record.SelfClass;
    public PlayerClass OppClass => Record.OppClass;
    public TurnOrder Order => Record.Order;
    public MatchResult Result => Record.Result;
    public MatchFormat Format => Record.Format;
    public DateTimeOffset StartedAt => Record.StartedAt;
    public DateTimeOffset EndedAt => Record.EndedAt;

    public string? SelfIconPath { get; private set; }
    public string? OppIconPath { get; private set; }

    private readonly SvgIconCache _cache = SvgIconCache.Instance;

    public HistoryRowViewModel(MatchRecord record)
    {
        Record = record;
        var map = Application.Current?.Resources["UiMap"] as UiMapProvider;
        if (map != null)
        {
            var selfKey = record.SelfClass.ToString();
            var oppKey = record.OppClass.ToString();
            var selfItem = map.Get($"Class.{record.SelfClass}");
            var oppItem = map.Get($"Class.{record.OppClass}");
            SelfIconPath = _cache.Get(selfKey, selfItem.iconUrl);
            OppIconPath = _cache.Get(oppKey, oppItem.iconUrl);
        }
        _cache.IconReady += OnIconReady;
    }

    private void OnIconReady(string key, string path)
    {
        var selfKey = Record.SelfClass.ToString();
        var oppKey = Record.OppClass.ToString();
        if (key == selfKey)
        {
            SelfIconPath = path;
            Raise(nameof(SelfIconPath));
        }
        if (key == oppKey)
        {
            OppIconPath = path;
            Raise(nameof(OppIconPath));
        }
    }

    public void Dispose()
    {
        _cache.IconReady -= OnIconReady;
    }
}
