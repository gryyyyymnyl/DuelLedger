namespace DuelLedger.Detectors.Shadowverse;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenCvSharp;
using DuelLedger.Vision;
using DuelLedger.Core;

public class FormatDetector : IStateDetector
{
    private readonly List<(string Label, Mat Tpl)> _templates = new();
    public GameState State => GameState.MatchStart;
    public string Message { get; private set; } = "{}";
    public IReadOnlyList<string> BestLabelsInGroups { get; private set; } = Array.Empty<string>();
    // テンプレート側：テンプレ画像を部分切り抜き
    private readonly (double x, double y, double w, double h) _tplRel = (0.00, 0.00, 1.00, 1.00);//0.02, 0.20, 0.25, 0.30

    /// <summary>
    /// 複数テンプレートをロード（ラベルはファイル名 stem を使用）
    /// </summary>
    public FormatDetector(IEnumerable<string> templatePaths)
    {
        foreach (var p in templatePaths)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            var img = Cv2.ImRead(p, ImreadModes.Grayscale);
            if (img.Empty()) continue;
            var label = Path.GetFileNameWithoutExtension(p) ?? "template";
            _templates.Add((label, img));
        }
    }

    public bool IsMatch(Mat screen, out double score, out OpenCvSharp.Point location)
    {
        score = 0; location = new OpenCvSharp.Point(0, 0);
        BestLabelsInGroups = Array.Empty<string>();
        if (screen.Empty() || _templates.Count == 0) return false;

        // 1) ラベル接頭辞「Group__」でグルーピング（未指定は G1）
        var groups = _templates
            .GroupBy(t => {
                var name = t.Label ?? "template";
                var idx = name.IndexOf("__", StringComparison.Ordinal);
                return (idx > 0) ? name.Substring(0, idx) : "G1";
            })
            .ToDictionary(g => g.Key, g => g.ToList());

        // 2) 各グループで“どれか1つ一致” → 全グループ満たしたら AND 成立
        double minGroupBest = double.PositiveInfinity;
        var bestLabels = new List<string>();
        string? bestLabelOverall = null;
        OpenCvSharp.Point bestLocOverall = default;

        foreach (var kv in groups)
        {
            bool anyInGroup = false;
            double bestInGroup = double.NegativeInfinity;
            OpenCvSharp.Point bestLocInGroup = default;
            string? bestLabelInGroup = null;

            foreach (var (label, tpl) in kv.Value)
            {
                var screenRect = ResolveScreenRectForLabel(label, screen.Width, screen.Height, VsElem.MatchFormat);
                var tplRR = new RelativeRegion(_tplRel.x, _tplRel.y, _tplRel.w, _tplRel.h, tpl.Width, tpl.Height);
                if (ImageMatch.TryOrbHomographyMatch(screen, tpl, screenRect, tplRR, out var s, out var loc))
                {
                    anyInGroup = true;
                    if (s > bestInGroup) { bestInGroup = s; bestLocInGroup = loc; bestLabelInGroup = label; }
                }
            }
            if (!anyInGroup) return false; // AND失敗
            // 採用ラベルを収集
            bestLabels.Add(bestLabelInGroup ?? "");
            if (bestInGroup < minGroupBest) { minGroupBest = bestInGroup; bestLabelOverall = bestLabelInGroup; bestLocOverall = bestLocInGroup; }
        }

        // 3) 全群クリア
        BestLabelsInGroups = bestLabels.AsReadOnly();
        score = double.IsPositiveInfinity(minGroupBest) ? 0 : minGroupBest;
        location = bestLocOverall;

        var labelsJoined = string.Join(", ", BestLabelsInGroups);
        var formatLabel = labelsJoined.Contains("format__Rank") ? "ランクマッチ"
                          : labelsJoined.Contains("format__2pick") ? "2Pick"
                          : labelsJoined;
        Message = JsonSerializer.Serialize(new { format = formatLabel });
#if DEBUG
        Console.WriteLine($"[Format] AND matched (groups={groups.Count})=> '{Message}', minScore={score:F3}, loc={location}, labels=[{string.Join(", ", BestLabelsInGroups)}]");
#endif
        return true;
    }

    // --- ラベルから個別 screenRect を解決（__roi / __elem） ---
    private static Rect ResolveScreenRectForLabel(string label, int w, int h, VsElem fallbackElem)
    {
        var roiIdx = label.IndexOf("__roi=", StringComparison.OrdinalIgnoreCase);
        if (roiIdx >= 0)
        {
            var seg = label.Substring(roiIdx + 6);
            var end = seg.IndexOf("__", StringComparison.Ordinal);
            var val = (end >= 0) ? seg.Substring(0, end) : seg;
            var parts = val.Split(',');
            if (parts.Length == 4 &&
                double.TryParse(parts[0], out var rx) &&
                double.TryParse(parts[1], out var ry) &&
                double.TryParse(parts[2], out var rw2) &&
                double.TryParse(parts[3], out var rh2))
            {
                int sx = Math.Max(0, (int)Math.Round(rx * w));
                int sy = Math.Max(0, (int)Math.Round(ry * h));
                int sw = Math.Max(0, (int)Math.Round(rw2 * w));
                int sh = Math.Max(0, (int)Math.Round(rh2 * h));
                return new Rect(sx, sy, sw, sh);
            }
        }
        var elemIdx = label.IndexOf("__elem=", StringComparison.OrdinalIgnoreCase);
        if (elemIdx >= 0)
        {
            var seg = label.Substring(elemIdx + 7);
            var end = seg.IndexOf("__", StringComparison.Ordinal);
            var val = (end >= 0) ? seg.Substring(0, end) : seg;
            if (Enum.TryParse<VsElem>(val, ignoreCase: true, out var elem))
                return VsUiMap.GetRect(elem, w, h);
        }
        return VsUiMap.GetRect(fallbackElem, w, h);
    }
}