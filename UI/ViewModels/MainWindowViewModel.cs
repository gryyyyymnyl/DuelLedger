using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Collections;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.ViewModels;

public sealed class MainWindowViewModel : NotifyBase
{
    private readonly MatchReaderService _reader;

    public ObservableCollection<HistoryRowViewModel> History { get; } = new();

    public DataGridCollectionView HistoryView { get; }

    private HistoryRowViewModel? _currentItem;

    private MatchFormat? _selectedFormat;
    public MatchFormat? SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            if (_selectedFormat != value)
            {
                _selectedFormat = value;
                Raise(nameof(SelectedFormat));
                HistoryView.Refresh();
                Recompute();
            }
        }
    }

    public ICommand SetFormatCommand { get; }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { Set(ref _selectedTabIndex, value); Raise(nameof(RateTextForActiveTab)); }
    }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        set => Set(ref _downloadProgress, value);
    }

    private bool _isDownloadingTemplates;
    public bool IsDownloadingTemplates
    {
        get => _isDownloadingTemplates;
        set => Set(ref _isDownloadingTemplates, value);
    }

    private IEnumerable<HistoryRowViewModel> FilteredHistoryVms
        => HistoryView.Cast<HistoryRowViewModel>();

    private IEnumerable<MatchRecord> FilteredHistory
        => FilteredHistoryVms.Select(x => x.Record);

    // 並びは進行中を最優先し、終了時刻・開始時刻で安定ソート（新しい順）
    public IEnumerable<HistoryRowViewModel> HistoryDesc => HistoryView
        .Cast<HistoryRowViewModel>()
        .OrderByDescending(x => x.IsCurrent)
        .ThenByDescending(x => x.Record.EndedAt.ToUnixTimeMilliseconds())
        .ThenByDescending(x => x.Record.StartedAt.ToUnixTimeMilliseconds());

    public IReadOnlyList<PlayerClass?> SelfClassOptions { get; }
        = new PlayerClass?[] { null }
            .Concat(Enum.GetValues<PlayerClass>().Where(c => c != PlayerClass.Unknown).Cast<PlayerClass?>())
            .ToList();

    private PlayerClass? _selectedSelfClass;
    public PlayerClass? SelectedSelfClass
    {
        get => _selectedSelfClass;
        set { Set(ref _selectedSelfClass, value); Recompute(); }
    }

    private Totals _overallTotals = new();
    public Totals OverallTotals { get => _overallTotals; set => Set(ref _overallTotals, value); }

    private ObservableCollection<ClassVsRow> _selfFilteredRows = new();
    public ObservableCollection<ClassVsRow> SelfFilteredRows
    {
        get => _selfFilteredRows; set => Set(ref _selfFilteredRows, value);
    }

    private Totals _selfTotals = new();
    public Totals SelfTotals { get => _selfTotals; set => Set(ref _selfTotals, value); }

    private static string FormatRate(Totals t)
    {
        var w = t.Wins;
        var l = t.Losses;
        var rate = (w + l) == 0 ? 0 : 100.0 * w / (w + l);
        return $"W:{w}/L:{l}/{rate:F1}%";
    }

    private static string FormatBarText(int wins, int losses)
    {
        var rate = (wins + losses) == 0 ? 0 : 100.0 * wins / (wins + losses);
        return $"{wins}/{losses} : {rate:F1}%";
    }

    public string OverallRateText => FormatRate(OverallTotals);
    public string SelfRateText => FormatRate(SelfTotals);
    public string RateTextForActiveTab => SelectedTabIndex == 0 ? OverallRateText : SelfRateText;

    public MainWindowViewModel(MatchReaderService reader)
    {
        _reader = reader;
        HistoryView = new DataGridCollectionView(History)
        {
            Filter = o => o is HistoryRowViewModel r && (SelectedFormat is null || r.Record.Format == SelectedFormat)
        };
        _reader.Items.CollectionChanged += (_, __) => { RebuildHistory(); HistoryView.Refresh(); Recompute(); };
        // 互換: どちらが発火しても拾う
        _reader.SnapshotUpdated += dto => OnSnapshot(dto);
        _reader.Snapshot += OnSnapshot;
        RebuildHistory();
        _reader.LoadInitial();
        SelectedSelfClass = null; // All
        SelectedFormat = null; // All
        SetFormatCommand = new RelayCommand<MatchFormat?>(fmt => SelectedFormat = fmt);
        Recompute();
    }

    private static IEnumerable<PlayerClass> AllOpponentClasses()
        => Enum.GetValues<PlayerClass>().Where(c => c != PlayerClass.Unknown);

    private void Recompute()
    {
        var hist = FilteredHistory.ToList();

        // Overall totals
        OverallTotals = new Totals
        {
            Wins = hist.Count(x => x.Result == MatchResult.Win),
            Losses = hist.Count(x => x.Result == MatchResult.Lose),
        };

        // Self-specific: 選択した自分クラスのみで、相手クラス別に集計
        var subset = SelectedSelfClass.HasValue
            ? hist.Where(x => x.SelfClass == SelectedSelfClass.Value).ToList()
            : hist;
        var selfRows = AllOpponentClasses()
            .Select(cls =>
            {
                var wins = subset.Count(x => x.OppClass == cls && x.Result == MatchResult.Win);
                var losses = subset.Count(x => x.OppClass == cls && x.Result == MatchResult.Lose);
                return new ClassVsRow
                {
                    Opponent = cls,
                    Wins = wins,
                    Losses = losses,
                    BarText = FormatBarText(wins, losses),
                };
            })
            .ToList();
        SelfFilteredRows = new ObservableCollection<ClassVsRow>(selfRows);
        SelfTotals = new Totals
        {
            Wins = subset.Count(x => x.Result == MatchResult.Win),
            Losses = subset.Count(x => x.Result == MatchResult.Lose),
        };

        Raise(nameof(HistoryDesc));
        Raise(nameof(OverallRateText));
        Raise(nameof(SelfRateText));
        Raise(nameof(RateTextForActiveTab));
    }

    private void RebuildHistory()
    {
        foreach (var vm in History.Where(x => x != _currentItem))
            vm.Dispose();
        History.Clear();
        if (_currentItem is not null)
            History.Add(_currentItem);
        foreach (var r in _reader.Items)
            History.Add(new HistoryRowViewModel(r));
        Raise(nameof(History));       // 念のため（History バインド向け）
        Raise(nameof(HistoryDesc));   // 並び替え済みコレクション
    }

    private void EnsureCurrentItem(MatchRecord rec)
    {
        if (_currentItem is null)
        {
            _currentItem = new HistoryRowViewModel(rec) { IsCurrent = true };
            History.Insert(0, _currentItem);
        }
        else
        {
            var idx = History.IndexOf(_currentItem);
            _currentItem.Dispose();
            var vm = new HistoryRowViewModel(rec) { IsCurrent = true };
            if (idx >= 0)
                History[idx] = vm;
            else
                History.Insert(0, vm);
            _currentItem = vm;
        }
    }

    private void OnSnapshot(MatchSnapshotDto dto)
    {
        if (dto.StartedAt is null)
            return;
        var rec = dto.ToDomain();
        if (_currentItem is null || _currentItem.Record.StartedAt != rec.StartedAt)
        {
            EnsureCurrentItem(rec);
        }
        else
        {
            EnsureCurrentItem(rec); // replace existing with updated values
        }

        if (dto.EndedAt is not null && rec.Result != MatchResult.Unknown)
        {
            if (_currentItem is not null)
            {
                _currentItem.IsCurrent = false;
                _currentItem = null;
            }
        }

        Raise(nameof(HistoryDesc));
        HistoryView.Refresh();
        Recompute();
    }
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    public RelayCommand(Action<T?> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}