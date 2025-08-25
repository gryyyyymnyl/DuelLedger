using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using OpenCvSharp;
using DuelLedger.Vision;
using DuelLedger.Contracts;

namespace DuelLedger.Core;
public class GameStateManager
{
    private GameState _currentState = GameState.Unknown;
    private readonly IGameStateDetectorSet _detectorSet;
    private readonly List<IStateDetector> _detectors;
    private readonly List<IStateDetector> _formatDetectors;
    private readonly List<IStateDetector> _matchStartDetectors;
    private readonly List<IStateDetector> _battleDetectors;
    private readonly List<IStateDetector> _resultDetectors;
    // ===== 試合形式の保持 =====
    private string? _lastFormatLabel; // 直前に検知した試合形式（次試合の開始時に引き継いで出力）
    // ===== クラス検出 多数決 =====
    private const int ClassVoteWindow = 3; //クラス検出試行数
    private readonly Queue<string> _ownClassVotes = new();
    private readonly Queue<string> _enemyClassVotes = new();
    public string? FinalOwnClass { get; private set; }
    public string? FinalEnemyClass { get; private set; }
    private readonly MatchAggregator _matchAgg;
    private readonly IScreenSource _screenSource;
    public GameStateManager(IGameStateDetectorSet detectorSet, IScreenSource screenSource, IMatchPublisher? publisher = null)
    {
        _detectorSet = detectorSet;
        _screenSource = screenSource;
        _detectors = detectorSet.CreateDetectors();
        // 名前ベースでグルーピング（必要に応じて調整）
        bool IsType(IStateDetector d, string name) => d.GetType().Name.Contains(name, StringComparison.OrdinalIgnoreCase);
        _formatDetectors = _detectors.Where(d => IsType(d, "FormatDetector")).ToList();
        _matchStartDetectors = _detectors.Where(d => IsType(d, "MatchStartDetector")).ToList();
        _battleDetectors = _detectors.Where(d => IsType(d, "BattleDetector")).ToList();
        _resultDetectors = _detectors.Where(d => IsType(d, "ResultDetector")).ToList();

        _matchAgg = new MatchAggregator(publisher ?? new NullPublisher());
    }

    public void Update()
    {
        if (!_screenSource.TryCapture(out var screen))
            return;
        using (screen)
        {
            Cv2.CvtColor(screen, screen, ColorConversionCodes.BGR2GRAY);

            double scale = 0.4; // 0.4〜0.6くらいが実務的にバランス良い
            Cv2.Resize(screen, screen, new OpenCvSharp.Size(),
                scale, scale, OpenCvSharp.InterpolationFlags.Area);

        // 状態マシン（各段階で指定の検知のみ実行）
        switch (_currentState)
        {

            case GameState.Unknown:
                // 開始段階：まずMatchStartを確認（優先）。無ければFormatのみを確認し続ける。
                if (TryDetect(_matchStartDetectors, screen, out var ms, out var msScore, out var msLoc))
                {
                    Console.WriteLine($"[{_detectorSet.GameName}] 開始→バトル開始検知 (score: {msScore:F3}, at: {msLoc})");
                    _matchAgg.OnMatchStarted(DateTimeOffset.UtcNow);
                    TrySetTurnOrderFromMatchStart(ms);
                    // ◆要件: InBattleへ遷移するタイミングで、直前に検知した試合形式を出力
                    if (!string.IsNullOrWhiteSpace(_lastFormatLabel))
                    {
                        _matchAgg.OnFormatDetected(_lastFormatLabel!);
                    }
                    _currentState = GameState.InBattle;
                    return;
                }
                if (TryDetect(_formatDetectors, screen, out var fmt, out var fScore, out var fLoc))
                {
                    // Formatのみ検知：状態は維持（Unknown＝開始待機のまま）
                    Console.WriteLine($"[{_detectorSet.GameName}] 開始: Format継続 (score: {fScore:F3}, at: {fLoc})");
                    // 形式を保持（次の開始時に出力するため）
                    var label = TryExtractFormat(fmt);
                    if (!string.IsNullOrWhiteSpace(label))
                        _lastFormatLabel = label!.Trim();
                    return;
                }
                Console.WriteLine($"[{_detectorSet.GameName}] 開始: 未検出、継続待機");
                return;
            case GameState.InBattle:
                if (TryDetect(_battleDetectors, screen, out var bt, out var bScore, out var bLoc))
                {
                    Console.WriteLine($"[{_detectorSet.GameName}] 開始→バトル開始検知 (score: {bScore:F3}, at: {bLoc})");
                    TryUpdateClassVotes(bt);
                    TryPublishClasses(bt);
                    _currentState = GameState.Result;
                    return;
                }
                Console.WriteLine($"[{_detectorSet.GameName}] クラス： 未検出、終了待機");
                _currentState = GameState.Result;
                return;
            /*
                                case GameState.InBattle:
                                    // 試合中：Battleのみ検知。外れたら終了段階へ遷移。
                                    if (TryDetect(_battleDetectors, screen, out var bt, out var bScore, out var bLoc))
                                    {
                                        Console.WriteLine($"[{_detectorSet.GameName}] 開始→バトル開始検知 (score: {bScore:F3}, at: {bLoc})");
                                        _currentState = GameState.Result;
                                        return;
                                    }
                                    Console.WriteLine($"[{_detectorSet.GameName}] バトル→終了へ遷移(Battle未検出)");
                                    _currentState = GameState.Result;
                                    return;
            */
            case GameState.Result:
                // 試合終了：Resultのみ検知。検知できたら開始待機に戻る（ループ）。
                if (TryDetect(_resultDetectors, screen, out var rs, out var rScore, out var rLoc))
                {
                    Console.WriteLine($"[{_detectorSet.GameName}] 試合終了検知 → 開始待機に戻ります。 (score: {rScore:F3}, at: {rLoc})");
                    TryFinishMatch(rs);
                    _currentState = GameState.Unknown;
                    return;
                }
                Console.WriteLine($"[{_detectorSet.GameName}] 終了: 未検出、継続待機");
                return;
        }
        }
    }

    private static bool TryDetect(IEnumerable<IStateDetector> detectors, Mat screen, out IStateDetector matched, out double score, out OpenCvSharp.Point loc)
    {
        foreach (var d in detectors)
        {
            if (d.IsMatch(screen, out score, out loc)) { matched = d; return true; }
        }
        matched = null!; score = 0; loc = default;
        return false;
    }

    // --- クラス多数決ロジック ---
    private void TryUpdateClassVotes(IStateDetector matched)
    {
        if (matched is not null && matched.GetType().Name.Contains("BattleDetector", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // BattleDetector.Message から own_class / enemy_class を取得
                var msgProp = matched.GetType().GetProperty("Message");
                var json = msgProp?.GetValue(matched) as string ?? "{}";
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var own = root.TryGetProperty("own_class", out var o)
                    ? o.ValueKind switch
                    {
                        JsonValueKind.String => o.GetString(),
                        JsonValueKind.Number => o.TryGetInt32(out var oi) ? oi.ToString() : o.GetDouble().ToString(),
                        _ => null
                    }
                    : null;
                var enemy = root.TryGetProperty("enemy_class", out var e)
                    ? e.ValueKind switch
                    {
                        JsonValueKind.String => e.GetString(),
                        JsonValueKind.Number => e.TryGetInt32(out var ei) ? ei.ToString() : e.GetDouble().ToString(),
                        _ => null
                    }
                    : null;

                if (!string.IsNullOrWhiteSpace(own))
                    EnqueueVote(_ownClassVotes, own!);
                if (!string.IsNullOrWhiteSpace(enemy))
                    EnqueueVote(_enemyClassVotes, enemy!);

                // 最終クラス未確定なら最頻値で確定
                if (FinalOwnClass is null && _ownClassVotes.Count >= ClassVoteWindow)
                {
                    FinalOwnClass = Mode(_ownClassVotes);
                    Console.WriteLine($"[ClassVote] FinalOwnClass={FinalOwnClass} (n={_ownClassVotes.Count})");
                }
                if (FinalEnemyClass is null && _enemyClassVotes.Count >= ClassVoteWindow)
                {
                    FinalEnemyClass = Mode(_enemyClassVotes);
                    Console.WriteLine($"[ClassVote] FinalEnemyClass={FinalEnemyClass} (n={_enemyClassVotes.Count})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClassVote] error: {ex.Message}");
            }
        }
    }

    private static void EnqueueVote(Queue<string> q, string value)
    {
        q.Enqueue(value);
        while (q.Count > ClassVoteWindow) q.Dequeue();
    }

    private static string Mode(IEnumerable<string> items)
        => items
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key) // 安定化
            .First().Key;

    // --- 追加: 先行/後攻の反映 ---
    private void TrySetTurnOrderFromMatchStart(IStateDetector matched)
    {
        var msgProp = matched.GetType().GetProperty("Message");
        var msg = msgProp?.GetValue(matched) as string ?? "";
        TurnOrder order = msg.Contains("先行") ? TurnOrder.先行
                          : msg.Contains("後攻") ? TurnOrder.後攻
                          : TurnOrder.Unknown;
        if (order != TurnOrder.Unknown)
            _matchAgg.OnTurnOrderDetected(order);
    }

    // --- 追加: クラス検出を Aggregator に反映 ---
    private void TryPublishClasses(IStateDetector matched)
    {
        try
        {
            var msgProp = matched.GetType().GetProperty("Message");
            var json = msgProp?.GetValue(matched) as string ?? "{}";
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var own = root.TryGetProperty("own_class", out var o) ? o.GetString() : null;
            var enemy = root.TryGetProperty("enemy_class", out var e) ? e.GetString() : null;
            _matchAgg.OnClassesDetected(NormalizeLabel(own), NormalizeLabel(enemy));
        }
        catch { /* noop */ }
    }

    // --- 追加: 試合終了の確定出力 ---
    private void TryFinishMatch(IStateDetector matched)
    {
        var msgProp = matched.GetType().GetProperty("Message");
        var msg = msgProp?.GetValue(matched) as string ?? "";
        MatchResult result =
            msg.Contains("WIN", StringComparison.OrdinalIgnoreCase) ? MatchResult.Win :
            msg.Contains("LOSE", StringComparison.OrdinalIgnoreCase) ? MatchResult.Lose :
            MatchResult.Unknown;
        _matchAgg.OnMatchEnded(result, DateTimeOffset.UtcNow);
    }

    // --- FormatDetector.Message から試合形式ラベルを抽出（キー名称は複数に対応） ---
    private static string? TryExtractFormat(IStateDetector detector)
    {
        try
        {
            var msgProp = detector.GetType().GetProperty("Message");
            var json = msgProp?.GetValue(detector) as string ?? "{}";
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("format", out var f)) return NormalizeLabel(f.GetString());
            if (root.TryGetProperty("match_format", out var mf)) return NormalizeLabel(mf.GetString());
            if (root.TryGetProperty("mode", out var md)) return NormalizeLabel(md.GetString());
            if (root.TryGetProperty("queue", out var q)) return NormalizeLabel(q.GetString());
            return null;
        }
        catch { return null; }
    }

    // --- ラベル正規化（ゲーム非依存） ---
    private static string NormalizeLabel(string? s)
        => string.IsNullOrWhiteSpace(s) ? "Unknown" : s.Trim();

}
