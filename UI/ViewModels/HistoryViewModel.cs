using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.ViewModels;

public sealed class HistoryViewModel : NotifyBase
{
    private readonly ObservableCollection<MatchRecord> _source;
    private readonly DispatcherTimer _timer;
    private double _pendingWidth;
    private const double Gap = 8;
    private const int MaxItems = 200;

    public ObservableCollection<HistoryTileVm> Items { get; } = new();

    public HistoryViewModel(ObservableCollection<MatchRecord> source)
    {
        _source = source;
        foreach (var r in source) Items.Add(new HistoryTileVm(r));
        _source.CollectionChanged += OnSourceChanged;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _timer.Tick += (_, __) => { _timer.Stop(); UpdateLayout(_pendingWidth); };
    }

    private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (MatchRecord r in e.NewItems)
            {
                var vm = new HistoryTileVm(r) { IsEntering = true };
                Items.Add(vm);
            }
        }
        if (e.OldItems != null)
        {
            foreach (MatchRecord r in e.OldItems)
            {
                var target = Items.FirstOrDefault(x => x.Record == r);
                if (target != null) Items.Remove(target);
            }
        }
        TrimExcess();
        ScheduleLayout(_pendingWidth);
    }

    private void TrimExcess()
    {
        while (Items.Count > MaxItems)
        {
            var oldest = Items.OrderBy(x => x.OrderKey).First();
            Items.Remove(oldest);
        }
    }

    public void ScheduleLayout(double viewportWidth)
    {
        _pendingWidth = viewportWidth;
        _timer.Stop();
        _timer.Start();
    }

    public void UpdateLayout(double viewportWidth)
    {
        _pendingWidth = viewportWidth;
        if (Items.Count == 0) return;
        var tileWidth = Items[0].Width;
        var tileHeight = Items[0].Height;
        var columns = Math.Max(1, (int)Math.Floor((viewportWidth + Gap) / (tileWidth + Gap)));
        var ordered = Items.OrderByDescending(x => x.OrderKey).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var vm = ordered[i];
            var col = i % columns;
            var row = i / columns;
            var left = col * (tileWidth + Gap);
            var top = row * (tileHeight + Gap);
            vm.Left = left;
            vm.Top = top;
        }
    }
}
