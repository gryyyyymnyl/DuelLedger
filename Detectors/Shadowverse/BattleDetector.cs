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

    public string Message { get; private set; } = "{}";

    private const int HistoryLength = 7;
    private static readonly double[] Scales = { 0.95, 1.00, 1.05 };
    private static readonly double[] Rots = { 0.0, 6.0, -6.0 };
    private static readonly HashSet<int> ComplexIds = new() { 3, 4, 7 };

    private readonly Dictionary<int, Queue<double>> _ownHist = new();
    private readonly Dictionary<int, Queue<double>> _enemyHist = new();

    public BattleDetector() { }

    public bool IsMatch(Mat screen, out double score, out OpenCvSharp.Point location)
    {
        score = 0; location = new OpenCvSharp.Point();
        if (screen.Empty()) return false;

        var ownRect = VsUiMap.GetRect(VsElem.MyClass, screen.Width, screen.Height);
        var enemyRect = VsUiMap.GetRect(VsElem.OppClass, screen.Width, screen.Height);

        using var ownRoi = new Mat(screen, ownRect);
        using var enemyRoi = new Mat(screen, enemyRect);

        bool ownOk = TryMatchClass(ownRoi, _ownHist, "own", out int ownId, out double ownS);
        bool enemyOk = TryMatchClass(enemyRoi, _enemyHist, "enemy", out int enemyId, out double enemyS);

        score = Math.Max(ownS, enemyS);
        location = ownRect.Location;

        var dict = new Dictionary<string, int>
        {
            ["own_class"] = ownOk ? ownId : 0,
            ["enemy_class"] = enemyOk ? enemyId : 0
        };
        Message = JsonSerializer.Serialize(dict);
        return ownOk || enemyOk;
    }

    private static Mat ToGray(Mat m)
    {
        if (m.Channels() == 1) return m.Clone();
        var g = new Mat();
        Cv2.CvtColor(m, g, ColorConversionCodes.BGR2GRAY);
        return g;
    }

    private bool TryMatchClass(Mat roi, Dictionary<int, Queue<double>> hist, string tag,
                               out int bestClass, out double bestScore)
    {
        bestClass = 0; bestScore = 0;
        using var roiGray = ToGray(roi);
        using var roiBin = new Mat();
        Cv2.Threshold(roiGray, roiBin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        double bestCurrent = double.NegativeInfinity;
        int bestIdCurrent = 0;
        double bestS1 = 0, bestS2 = 0, bestS3 = 0, bestS = 0;

        for (int id = 1; id <= 7; id++)
        {
            var tpl = SvgTemplateCache.Get(id, roiGray.Width, roiGray.Height);
            double s1 = 0; OpenCvSharp.Point _;
            ImageMatch.ChamferZNCC(roiGray, tpl.Dist, out s1, out _, Scales, Rots);
            double s2 = ImageMatch.MatchShapesScore(roiBin, tpl.Binary);
            double s3 = 0;
            bool complex = ComplexIds.Contains(id);
            if (complex) ImageMatch.TryOrbHomographyMatch(roiGray, tpl.Gray, out s3);
            double S = complex ? 0.60 * s1 + 0.25 * s2 + 0.15 * s3 : 0.75 * s1 + 0.25 * s2;
            if (!(s1 >= 0.55 || S >= 0.60)) S = 0;
            UpdateHistory(hist, id, S);
            if (S > bestCurrent)
            {
                bestCurrent = S; bestIdCurrent = id;
                bestS1 = s1; bestS2 = s2; bestS3 = s3; bestS = S;
            }
        }

        double bestSum = double.NegativeInfinity;
        double latest = 0;
        int chosen = 0;
        foreach (var kv in hist)
        {
            double sum = kv.Value.Sum();
            double last = kv.Value.LastOrDefault();
            if (sum > bestSum || (Math.Abs(sum - bestSum) < 1e-6 && last > latest))
            {
                bestSum = sum; latest = last; chosen = kv.Key;
            }
        }
        bestClass = chosen;
        bestScore = latest;

        try
        {
            var outDir = Path.Combine(AppContext.BaseDirectory, "out");
            Directory.CreateDirectory(outDir);
            var tplDbg = SvgTemplateCache.Get(bestIdCurrent, roiGray.Width, roiGray.Height);
            SaveDebug(tag, roiGray, tplDbg, bestS1, bestS2, bestS3, bestS, outDir);
        }
        catch { }

        return bestClass != 0;
    }

    private static void UpdateHistory(Dictionary<int, Queue<double>> hist, int id, double val)
    {
        if (!hist.TryGetValue(id, out var q))
        {
            q = new Queue<double>();
            hist[id] = q;
        }
        q.Enqueue(val);
        while (q.Count > HistoryLength) q.Dequeue();
    }

    private static void SaveDebug(string tag, Mat roiGray, SvgTemplate tpl,
                                  double s1, double s2, double s3, double S, string dir)
    {
        using var vis = new Mat();
        Cv2.CvtColor(roiGray, vis, ColorConversionCodes.GRAY2BGR);
        var text = $"s1={s1:F2} s2={s2:F2} s3={s3:F2} S={S:F2}";
        Cv2.PutText(vis, text, new OpenCvSharp.Point(2, 15), HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 255, 0), 1);
        Cv2.ImWrite(Path.Combine(dir, $"{tag}_roi_gray.png"), vis);
        Cv2.ImWrite(Path.Combine(dir, $"{tag}_tpl_binary.png"), tpl.Binary);
        using var distVis = new Mat();
        Cv2.Normalize(tpl.Dist, distVis, 0, 255, NormTypes.MinMax);
        distVis.ConvertTo(distVis, MatType.CV_8U);
        Cv2.ImWrite(Path.Combine(dir, $"{tag}_tpl_dist.png"), distVis);
    }
}

