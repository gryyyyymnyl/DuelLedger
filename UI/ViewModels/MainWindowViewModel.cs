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
    private readonly MatchStateService _state;

    public ObservableCollection<HistoryRowViewModel> History { get; } = new();

    public DataGridCollectionView HistoryView { get; }

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

    public bool IsInMatch => _state.IsInMatch;
    // 互換用：XAMLが IsInBattle を参照している場合に対応
    public bool IsInBattle => IsInMatch;

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

    // 降順（新しい順）の履歴ビュー（時系列降順）
    // 時系列（新しい順）: UTC基準 + 複合キーで安定ソート
    public IEnumerable<HistoryRowViewModel> HistoryDesc => FilteredHistoryVms
        // 完全に「時刻のみ」で安定ソート（新しい順）
        .OrderByDescending(x => x.Record.EndedAt.ToUnixTimeMilliseconds())
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

    public MainWindowViewModel(MatchReaderService reader, MatchStateService state)
    {
        _reader = reader;
        _state = state;
        _state.PropertyChanged += (_, __) =>
        {
            Raise(nameof(IsInMatch));
            Raise(nameof(IsInBattle));
        };
        _reader.LoadInitial();
        RebuildHistory();
        HistoryView = new DataGridCollectionView(History)
        {
            Filter = o => o is HistoryRowViewModel r && (SelectedFormat is null || r.Record.Format == SelectedFormat)
        };
        _reader.Items.CollectionChanged += (_, __) => { RebuildHistory(); HistoryView.Refresh(); Recompute(); };
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
        foreach (var vm in History)
            vm.Dispose();
        History.Clear();
        foreach (var r in _reader.Items)
            History.Add(new HistoryRowViewModel(r));
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