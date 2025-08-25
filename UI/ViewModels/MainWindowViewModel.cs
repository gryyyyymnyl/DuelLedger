using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.ViewModels;

public sealed class MainWindowViewModel : NotifyBase
{
    private readonly MatchReaderService _reader;

    public ObservableCollection<MatchRecord> History => _reader.Items;

    public ObservableCollection<HistoryTile> HistoryDesc { get; } = new();

    private readonly Dictionary<MatchRecord, HistoryTile> _tileMap = new();

    private double _canvasWidth;
    public double CanvasWidth
    {
        get => _canvasWidth;
        set { Set(ref _canvasWidth, value); UpdateTilePositions(); }
    }

    private const double TileWidth = 110;
    private const double TileHeight = 110;
    private const double TileSpacing = 8;
    private const int MaxHistoryItems = 200;

    private double _canvasHeight;
    public double CanvasHeight
    {
        get => _canvasHeight;
        private set => Set(ref _canvasHeight, value);
    }

    public IReadOnlyList<MatchFormat?> AvailableFormats { get; }
        = new MatchFormat?[] { null, MatchFormat.Rank, MatchFormat.TwoPick, MatchFormat.GrandPrix };

    private MatchFormat? _selectedFormat;
    public MatchFormat? SelectedFormat
    {
        get => _selectedFormat;
        set { Set(ref _selectedFormat, value); Recompute(); }
    }

    private IEnumerable<MatchRecord> FilteredHistory
        => SelectedFormat.HasValue ? History.Where(x => x.Format == SelectedFormat.Value) : History;

    private static IEnumerable<MatchRecord> SortDesc(IEnumerable<MatchRecord> src)
        => src.OrderByDescending(x => x.EndedAt)
               .ThenByDescending(x => x.StartedAt)
               .ThenByDescending(x => x.Result)
               .ThenBy(x => x.SelfClass)
               .ThenBy(x => x.OppClass)
               .ThenBy(x => x.Order);

    public IReadOnlyList<PlayerClass> SelfClassOptions { get; } = Enum.GetValues<PlayerClass>().Where(c => c != PlayerClass.Unknown).ToList();

    private PlayerClass _selectedSelfClass;
    public PlayerClass SelectedSelfClass
    {
        get => _selectedSelfClass;
        set { Set(ref _selectedSelfClass, value); Recompute(); }
    }

    private ObservableCollection<ClassVsRow> _overallRows = new();
    public ObservableCollection<ClassVsRow> OverallRows
    {
        get => _overallRows; set => Set(ref _overallRows, value);
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

        public MainWindowViewModel(MatchReaderService reader)
    {
        _reader = reader;
        _reader.Items.CollectionChanged += (_, __) => Recompute();
        SelectedSelfClass = SelfClassOptions.FirstOrDefault();
        SelectedFormat = null; // All
        _reader.LoadInitial();
        Recompute();
    }

    private static IEnumerable<PlayerClass> AllOpponentClasses()
        => Enum.GetValues<PlayerClass>().Where(c => c != PlayerClass.Unknown);

    private void Recompute()
    {
        var hist = SortDesc(FilteredHistory).Take(MaxHistoryItems).ToList();

        // Overall: 相手クラス別に勝敗集計（自分クラスは全体）
        var overall = AllOpponentClasses()
            .Select(cls => new ClassVsRow
            {
                Opponent = cls,
                Wins = hist.Count(x => x.OppClass == cls && x.Result == MatchResult.Win),
                Losses = hist.Count(x => x.OppClass == cls && x.Result == MatchResult.Lose),
            })
            .ToList();
        OverallRows = new ObservableCollection<ClassVsRow>(overall);
        OverallTotals = new Totals
        {
            Wins = hist.Count(x => x.Result == MatchResult.Win),
            Losses = hist.Count(x => x.Result == MatchResult.Lose),
        };

        // Self-specific: 選択した自分クラスのみで、相手クラス別に集計
        var subset = hist.Where(x => x.SelfClass == SelectedSelfClass).ToList();
        var selfRows = AllOpponentClasses()
            .Select(cls => new ClassVsRow
            {
                Opponent = cls,
                Wins = subset.Count(x => x.OppClass == cls && x.Result == MatchResult.Win),
                Losses = subset.Count(x => x.OppClass == cls && x.Result == MatchResult.Lose),
            })
            .ToList();
        SelfFilteredRows = new ObservableCollection<ClassVsRow>(selfRows);
        SelfTotals = new Totals
        {
            Wins = subset.Count(x => x.Result == MatchResult.Win),
            Losses = subset.Count(x => x.Result == MatchResult.Lose),
        };

        // sync HistoryDesc collection
        for (int i = HistoryDesc.Count - 1; i >= 0; i--)
        {
            if (!hist.Contains(HistoryDesc[i].Record))
            {
                _tileMap.Remove(HistoryDesc[i].Record);
                HistoryDesc.RemoveAt(i);
            }
        }
        foreach (var rec in hist)
        {
            if (!_tileMap.TryGetValue(rec, out var tile))
            {
                tile = new HistoryTile(rec);
                _tileMap[rec] = tile;
                HistoryDesc.Add(tile);
            }
        }

        _histOrder = hist;
        UpdateTilePositions();
    }

    private List<MatchRecord> _histOrder = new();

    private void UpdateTilePositions()
    {
        if (CanvasWidth <= 0 || _histOrder.Count == 0) return;
        int cols = Math.Max(1, (int)((CanvasWidth + TileSpacing) / (TileWidth + TileSpacing)));
        for (int i = 0; i < _histOrder.Count; i++)
        {
            var tile = _tileMap[_histOrder[i]];
            var col = i % cols;
            var row = i / cols;
            tile.Left = col * (TileWidth + TileSpacing);
            tile.Top = row * (TileHeight + TileSpacing);
        }
        int rows = (int)Math.Ceiling((double)_histOrder.Count / cols);
        CanvasHeight = rows * (TileHeight + TileSpacing);
    }
}