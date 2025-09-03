namespace DuelLedger.Detectors.Shadowverse;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenCvSharp;
using DuelLedger.Vision;
using DuelLedger.Core;

public class BattleDetector : IStateDetector
{
    public GameState State => GameState.InBattle;

    public ImageMatch.OrbMatchConfig MatchConfig { get; set; } = new ImageMatch.OrbMatchConfig
    {
        MinInliers = 8,
        MinInlierRatio = 0.32,
        FastThreshold = 10,
        RatioTest = 0.75,
        RansacReprojErr = 3.0,
        MinKeypointsPerSide = 20
    };

    // クラス検出用テンプレ群（side × class名 → (Label,Tpl) 配列）
    private readonly Dictionary<string, List<(string Label, Mat Tpl)>> _ownClassTpls = new();
    private readonly Dictionary<string, List<(string Label, Mat Tpl)>> _enemyClassTpls = new();
    // 検出結果（辞書形式JSONを格納）
    public string Message { get; private set; } = "{}";
    // AND成立時に各グループで採用されたラベル（自陣/敵陣）
    public IReadOnlyList<string> OwnBestLabelsInGroups { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> EnemyBestLabelsInGroups { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// 相対位置は配列、テンプレ画像も配列で受け取り（将来slot追加も同パターンで拡張可）
    /// </summary>
    public BattleDetector(IEnumerable<string> ownClassTemplatePaths,
                          IEnumerable<string> enemyClassTemplatePaths)
    {
        void AddTpl(Dictionary<string, List<(string Label, Mat Tpl)>> bank, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            using var img = Cv2.ImRead(path, ImreadModes.Grayscale);
            if (img.Empty())
            {
                Console.WriteLine($"[Battle] Skip (imread empty): {path}");
                return;
            }
            var label = Path.GetFileNameWithoutExtension(path) ?? "";
            var cls = MapClass(label);
            if (cls == null)
            {
                Console.WriteLine($"[Battle] Skip (class unmapped): '{label}'");
                return;
            }
            if (!bank.TryGetValue(cls, out var list)) bank[cls] = list = new List<(string Label, Mat Tpl)>();
            list.Add((label, img.Clone())); // ラベル保持で所有化
        }
        foreach (var p in ownClassTemplatePaths ?? Array.Empty<string>()) AddTpl(_ownClassTpls, p);
        foreach (var p in enemyClassTemplatePaths ?? Array.Empty<string>()) AddTpl(_enemyClassTpls, p);
    }

    public bool IsMatch(Mat screen, out double score, out OpenCvSharp.Point location)
    {
        score = 0; location = new OpenCvSharp.Point(0, 0);
        OwnBestLabelsInGroups = Array.Empty<string>();
        EnemyBestLabelsInGroups = Array.Empty<string>();
#if DEBUG
        Console.WriteLine("[Battle] IsMatch: start");
#endif
        if (screen.Empty())
        {
#if DEBUG
            Console.WriteLine("[Battle] IsMatch: return (screen.Empty)");
#endif
            return false;
        }
        if (_ownClassTpls.Count == 0 && _enemyClassTpls.Count == 0)
        {
#if DEBUG
            Console.WriteLine("[Battle] IsMatch: return (no class templates)");
#endif
            return false;
        }
        var _ownClassRegion = VsUiMap.GetRect(VsElem.MyClass, screen.Width, screen.Height);
        var _enemyClassRegion = VsUiMap.GetRect(VsElem.OppClass, screen.Width, screen.Height);

#if DEBUG
        Console.WriteLine($"[Battle] IsMatch: ownRegion={_ownClassRegion}, enemyRegion={_enemyClassRegion}");
        Console.WriteLine($"[Battle] IsMatch: ownTpls={_ownClassTpls.Sum(kv => kv.Value.Count)} ({string.Join(',', _ownClassTpls.Keys)}), enemyTpls={_enemyClassTpls.Sum(kv => kv.Value.Count)} ({string.Join(',', _enemyClassTpls.Keys)})");
#endif
        // 側ごとにクラスを推定（クラス内で Group AND、グループ内 OR）
        var ownOk = TryMatchClass(screen, _ownClassRegion, _ownClassTpls,
                                  out var ownCls, out var ownScore, out var ownLoc, out var ownLabels,
                                  debugPrefix: "own", cfg: MatchConfig);
        var enemyOk = TryMatchClass(screen, _enemyClassRegion, _enemyClassTpls,
                                    out var enemyCls, out var enemyScore, out var enemyLoc, out var enemyLabels,
                                    debugPrefix: "enemy", cfg: MatchConfig);
        if (!(ownOk || enemyOk))
        {
#if DEBUG
            Console.WriteLine("[Battle] IsMatch: return (no match on both sides)");
#endif
            return false;
        }
        // 代表スコア/座標（優先: 自分→相手）
        if (ownOk) { score = ownScore; location = ownLoc; OwnBestLabelsInGroups = ownLabels.AsReadOnly(); }
        else { score = enemyScore; location = enemyLoc; }
        if (enemyOk) EnemyBestLabelsInGroups = enemyLabels.AsReadOnly();
        // message に辞書形式（JSON）で格納
        var dict = new Dictionary<string, string>
        {
            ["own_class"] = ownCls ?? "",
            ["enemy_class"] = enemyCls ?? ""
        };
        Message = JsonSerializer.Serialize(dict);

#if DEBUG
        Console.WriteLine($"[Battle] own={ownCls ?? "-"}({ownScore:F3}) labels=[{string.Join(", ", OwnBestLabelsInGroups)}]");
        Console.WriteLine($"[Battle] enemy={enemyCls ?? "-"}({enemyScore:F3}) labels=[{string.Join(", ", EnemyBestLabelsInGroups)}]");
        Console.WriteLine($"[Battle] Message={Message}");
#endif
        return true;
    }

    // --- 内部実装 ---
    private static bool TryMatchClass(Mat screen, Rect fallbackRegion,
                                      Dictionary<string, List<(string Label, Mat Tpl)>> bank,
                                      out string? bestClass, out double bestScore,
                                      out OpenCvSharp.Point bestLoc, out List<string> bestLabelsInGroups,
                                      string? debugPrefix = null,
                                        ImageMatch.OrbMatchConfig? cfg = null)
    {
        bestClass = null; bestScore = double.NegativeInfinity; bestLoc = default;
        bestLabelsInGroups = new List<string>();
#if DEBUG
        Console.WriteLine($"[Battle] TryMatchClass: region scan start region={fallbackRegion}");
#endif
        foreach (var (cls, mats) in bank)
        {
#if DEBUG
            Console.WriteLine($"[Battle]   class='{cls}' mats={mats.Count}");
#endif
            // --- クラス内でグループAND（接頭辞 "<G>__"） ---
            var groups = mats.GroupBy(t =>
            {
                var name = t.Label ?? "tpl";
                var idx = name.IndexOf("__", StringComparison.Ordinal);
                return (idx > 0) ? name.Substring(0, idx) : "G1";
            }).ToDictionary(g => g.Key, g => g.ToList());

            bool classOk = true;
            double classScore = double.PositiveInfinity; // minで縮約
            OpenCvSharp.Point classLoc = default;
            var chosenLabels = new List<string>();

            foreach (var kv in groups)
            {
                bool anyInGroup = false;
                double bestInGroup = double.NegativeInfinity;
                OpenCvSharp.Point bestLocInGroup = default;
                string? bestLabelInGroup = null;

                foreach (var (label, tpl) in kv.Value)
                {
                    // ラベルごとに screenRect を上書き（__roi / __elem）なければ fallbackRegion
                    var screenRect = ResolveScreenRectForLabel(label, screen.Width, screen.Height, fallbackRegion);
                    var tplRR = new RelativeRegion(0, 0, 1, 1, tpl.Width, tpl.Height);
                    var dbgName = string.IsNullOrEmpty(debugPrefix) ? null : $"{debugPrefix}_{label}";
                    if (ImageMatch.TryOrbHomographyMatch(screen, tpl, screenRect, tplRR,
                                                         out var s, out var loc, cfg: cfg,
                                                         callerName: dbgName))
                    //, canny1: 170, canny2: 400, scales: new[] { 1.0 }, new(@"C:\Users\MW\Documents\Projects\SWBT\bin\Debug\net8.0-windows\out", $"own_icon_{cls}")))
                    {
                        anyInGroup = true;
                        if (s > bestInGroup) { bestInGroup = s; bestLocInGroup = loc; bestLabelInGroup = label; }
                    }
                }
                if (!anyInGroup) { classOk = false; break; }
                chosenLabels.Add(bestLabelInGroup ?? "");
                if (bestInGroup < classScore) { classScore = bestInGroup; classLoc = bestLocInGroup; }
            }

            if (classOk && classScore > bestScore)
            {
                bestScore = classScore;
                bestLoc = classLoc;
                bestClass = cls;
                bestLabelsInGroups = chosenLabels;
#if DEBUG
                Console.WriteLine($"[Battle]     CLASS HIT '{cls}' minGroupScore={classScore:F3} labels=[{string.Join(", ", chosenLabels)}]");
#endif
            }
        }
#if DEBUG
        if (bestClass is null) Console.WriteLine("[Battle] TryMatchClass: no hit");
        else Console.WriteLine($"[Battle] TryMatchClass: best class='{bestClass}' score={bestScore:F3} loc={bestLoc}");
#endif
        return bestClass != null;
    }

    private static string? MapClass(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("sword") || name.Contains("ロイヤル")) return "ロイヤル";
        if (lower.Contains("forest") || name.Contains("エルフ")) return "エルフ";
        if (lower.Contains("rune") || name.Contains("ウィッチ")) return "ウィッチ";
        if (lower.Contains("dragon") || name.Contains("ドラゴン")) return "ドラゴン";
        if (lower.Contains("abyss") || name.Contains("ナイトメア")) return "ナイトメア";
        if (lower.Contains("haven") || name.Contains("ビショップ")) return "ビショップ";
        if (lower.Contains("portal") || name.Contains("ネメシス")) return "ネメシス";
        return null;
    }

    // ラベルから個別 screenRect を解決： __roi=x,y,w,h（相対） > __elem=Name > fallbackRect
    private static Rect ResolveScreenRectForLabel(string label, int w, int h, Rect fallbackRect)
    {
        var elemIdx = label.IndexOf("__elem=", StringComparison.OrdinalIgnoreCase);
        if (elemIdx >= 0)
        {
            var seg = label.Substring(elemIdx + 7);
            var end = seg.IndexOf("__", StringComparison.Ordinal);
            var val = (end >= 0) ? seg.Substring(0, end) : seg;

            if (Enum.TryParse<VsElem>(val, ignoreCase: true, out var elem))
            {
                if (elem == VsElem.MyClass)
                {
                    // 画面左 40%、上から 5%〜高さ 60%
                    return new Rect(0, (int)(0.05 * h), (int)(0.50 * w), (int)(0.65 * h));
                }
                if (elem == VsElem.OppClass)
                {
                    // 画面右 40%、上から 5%〜高さ 60%
                    int roiW = (int)(0.50 * w);
                    int roiH = (int)(0.65 * h);
                    return new Rect(w - roiW, (int)(0.05 * h), roiW, roiH);
                }

                // それ以外は従来通り VsUiMap を使う
                return VsUiMap.GetRect(elem, w, h);
            }
        }
        return fallbackRect;
    }
}