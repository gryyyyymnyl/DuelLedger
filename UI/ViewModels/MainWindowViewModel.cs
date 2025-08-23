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

    // 降順（新しい順）の履歴ビュー（時系列降順）
    // 時系列（新しい順）: UTC基準 + 複合キーで安定ソート
    public IEnumerable<MatchRecord> HistoryDesc => History
        // 完全に「時刻のみ」で安定ソート（新しい順）
        .OrderByDescending(x => x.EndedAt.ToUnixTimeMilliseconds())
        .ThenByDescending(x => x.StartedAt.ToUnixTimeMilliseconds());

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

        public MainWindowViewModel()
    {
        _reader = new MatchReaderService();
        _reader.Items.CollectionChanged += (_, __) => Recompute();
        SelectedSelfClass = SelfClassOptions.FirstOrDefault();
        _reader.LoadInitial();
        Recompute();
    }

    private static IEnumerable<PlayerClass> AllOpponentClasses()
        => Enum.GetValues<PlayerClass>().Where(c => c != PlayerClass.Unknown);

    private void Recompute()
    {
        // Overall: 相手クラス別に勝敗集計（自分クラスは全体）
        var overall = AllOpponentClasses()
            .Select(cls => new ClassVsRow
            {
                Opponent = cls,
                Wins = History.Count(x => x.OppClass == cls && x.Result == MatchResult.Win),
                Losses = History.Count(x => x.OppClass == cls && x.Result == MatchResult.Lose),
            })
            .ToList();
        OverallRows = new ObservableCollection<ClassVsRow>(overall);
        OverallTotals = new Totals
        {
            Wins = History.Count(x => x.Result == MatchResult.Win),
            Losses = History.Count(x => x.Result == MatchResult.Lose),
        };

        // Self-specific: 選択した自分クラスのみで、相手クラス別に集計
        var subset = History.Where(x => x.SelfClass == SelectedSelfClass).ToList();
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

        

        Raise(nameof(History));
        Raise(nameof(HistoryDesc));
    }
}