using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Features2D;
namespace DuelLedger.Vision
{
    public static class ImageMatch
    {
        public sealed class OrbMatchConfig
        {
            public int MinKeypointsPerSide { get; init; } = 20;
            public double RatioTest { get; init; } = 0.75;
            public double RansacReprojErr { get; init; } = 3.0;
            public int MinInliers { get; init; } = 12;
            public double MinInlierRatio { get; init; } = 0.45;
            public int NFeatures { get; init; } = 1000;
            public float ScaleFactor { get; init; } = 1.2f;
            public int NLevels { get; init; } = 8;
            public int EdgeThreshold { get; init; } = 31;
            public int FirstLevel { get; init; } = 0;
            public int WtaK { get; init; } = 2;
            public ORBScoreType ScoreType { get; init; } = ORBScoreType.Harris;
            public int PatchSize { get; init; } = 31;
            public int FastThreshold { get; init; } = 20;
        }

        // -------------- 共通：特徴量マッチ用コア --------------
        private static bool TryFeatureHomographyMatch(
            Mat screen, Mat template,
            Rectangle screenRect, RelativeRegion templateRegion,
            Func<Feature2D> createDetector, NormTypes matcherNorm,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            score = 0; location = new OpenCvSharp.Point(0, 0);
            if (screen.Empty() || template.Empty()) return false;
            cfg ??= new OrbMatchConfig();

            if (!TryCrop(screen, screenRect, out var imgCrop, out var imgRoi)) return false;
            try
            {
                var tplRect = templateRegion.ComputeRect(
                    template.Width, template.Height,
                    RelativeRegion.Anchor2D.CenterX | RelativeRegion.Anchor2D.CenterY,
                    RelativeRegion.ScaleMode.Stretch
                );
                if (tplRect.Width <= 0 || tplRect.Height <= 0 ||
                    tplRect.Right > template.Width || tplRect.Bottom > template.Height) return false;

                var tplRectCv = new OpenCvSharp.Rect(tplRect.X, tplRect.Y, tplRect.Width, tplRect.Height);
                using var tplCrop = new Mat(template, tplRectCv);

                using var detector = createDetector();
                KeyPoint[] kpsImg, kpsTpl;
                using var descImg = new Mat();
                using var descTpl = new Mat();
                detector.DetectAndCompute(imgCrop, null, out kpsImg, descImg);
                detector.DetectAndCompute(tplCrop, null, out kpsTpl, descTpl);

                if (kpsImg.Length < cfg.MinKeypointsPerSide || kpsTpl.Length < cfg.MinKeypointsPerSide ||
                    descImg.Empty() || descTpl.Empty()) return false;

                using var matcher = new BFMatcher(matcherNorm, crossCheck: false);
                var knn = matcher.KnnMatch(descTpl, descImg, k: 2);
                var good = knn.Where(p => p.Length >= 2 && p[0].Distance < cfg.RatioTest * p[1].Distance)
                              .Select(p => p[0]).ToList();
                if (good.Count < cfg.MinInliers) return false;

                var srcPts = good.Select(m => kpsTpl[m.QueryIdx].Pt).ToArray();
                var dstPts = good.Select(m => kpsImg[m.TrainIdx].Pt).ToArray();
                using var srcIA = InputArray.Create(srcPts);
                using var dstIA = InputArray.Create(dstPts);
                using var inlierMask = new Mat();
                using var H = Cv2.FindHomography(srcIA, dstIA, HomographyMethods.Ransac, cfg.RansacReprojErr, inlierMask);
                if (H.Empty() || inlierMask.Empty()) return false;

                int inliers = 0;
                for (int i = 0; i < inlierMask.Rows; i++) if (inlierMask.Get<byte>(i, 0) != 0) inliers++;
                double inlierRatio = inliers / (double)good.Count;
                score = inlierRatio;

                var tplCorners = new[]
                {
                    new Point2f(0,0),
                    new Point2f(tplCrop.Width-1,0),
                    new Point2f(tplCrop.Width-1,tplCrop.Height-1),
                    new Point2f(0,tplCrop.Height-1)
                };
                var projected = Cv2.PerspectiveTransform(tplCorners, H);
                double minX = projected.Min(p => p.X);
                double minY = projected.Min(p => p.Y);
                location = new OpenCvSharp.Point((int)(minX + imgRoi.X), (int)(minY + imgRoi.Y));

#if DEBUG
                if ((inliers >= cfg.MinInliers) && (inlierRatio >= cfg.MinInlierRatio))
                {
                    try
                    {
                        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
                        Directory.CreateDirectory(outDir);
                        var file = $"debug_{(string.IsNullOrWhiteSpace(callerName) ? "unknown" : callerName)}.jpg";
                        var baseName = string.IsNullOrWhiteSpace(callerName) ? "unknown" : callerName;
                        var pathVis = Path.Combine(outDir, $"debug_{baseName}.jpg");
                        var pathCrop = Path.Combine(outDir, $"debug_{baseName}_crop.jpg");   // 画面側切り抜き
                        var pathTpl = Path.Combine(outDir, $"debug_{baseName}_tpl.jpg");    // テンプレ側切り抜き
                        using var vis = (screen.Channels() == 1) ? new Mat() : screen.Clone();
                        if (screen.Channels() == 1) Cv2.CvtColor(screen, vis, ColorConversionCodes.GRAY2BGR);
                        var pts = new[]
                        {
                            new OpenCvSharp.Point((int)(projected[0].X + imgRoi.X), (int)(projected[0].Y + imgRoi.Y)),
                            new OpenCvSharp.Point((int)(projected[1].X + imgRoi.X), (int)(projected[1].Y + imgRoi.Y)),
                            new OpenCvSharp.Point((int)(projected[2].X + imgRoi.X), (int)(projected[2].Y + imgRoi.Y)),
                            new OpenCvSharp.Point((int)(projected[3].X + imgRoi.X), (int)(projected[3].Y + imgRoi.Y)),
                        };
                        Cv2.Polylines(vis, new[] { pts }, true, new Scalar(0, 255, 0), 2);
                        Cv2.Circle(vis, pts[0], 4, new Scalar(255, 0, 0), -1);
                        var text = $"score={inlierRatio:F3} inliers={inliers}/{good.Count}";
                        Cv2.PutText(vis, text, new OpenCvSharp.Point(10, 30), HersheyFonts.HersheySimplex, 0.8, new Scalar(0, 255, 0), 2);
                        Cv2.ImWrite(pathVis, vis, new[] { (int)ImwriteFlags.JpegQuality, 70 });

                        // 追加：成功時に実際に用いた切り抜き画像を保存（トラブルシュート用）
                        // 画面側ROI
                        try { Cv2.ImWrite(pathCrop, imgCrop, new[] { (int)ImwriteFlags.JpegQuality, 80 }); } catch { }
                        // テンプレート側ROI
                        try { Cv2.ImWrite(pathTpl, tplCrop, new[] { (int)ImwriteFlags.JpegQuality, 80 }); } catch { }
                    }
                    catch { }
                }
#endif
                return (inliers >= cfg.MinInliers) && (inlierRatio >= cfg.MinInlierRatio);
            }
            finally
            {
                imgCrop.Dispose();
            }
        }

        /// <summary>System.Drawing.Rectangleで安全に切り出し（画面外ならfalse）</summary>
        private static bool TryCrop(Mat screen, Rectangle rect, out Mat crop, out Rect roi)
        {
            crop = null!;
            roi = default;
            if (rect.Width <= 0 || rect.Height <= 0) return false;
            // 画面内にクリップ
            int x = Math.Max(0, rect.X);
            int y = Math.Max(0, rect.Y);
            int w = Math.Min(rect.Right, screen.Width) - x;
            int h = Math.Min(rect.Bottom, screen.Height) - y;
            if (w <= 0 || h <= 0) return false;
            roi = new Rect(x, y, w, h);
            crop = new Mat(screen, roi);
            return true;
        }

        // --- 追加: RelativeRegion をそのまま渡せるオーバーロード ---
        public static bool TryOrbHomographyMatch(
            Mat screen, Mat template,
            RelativeRegion screenRegion, RelativeRegion.Anchor2D screenAnchor, RelativeRegion.ScaleMode screenScale,
            RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            // 画面側は毎フレームのサイズから実ピクセル矩形に変換
            var screenRect = screenRegion.ComputeRect(
                screen.Width, screen.Height,
                screenAnchor, screenScale);
            // 既存実装にフォワード
            return TryOrbHomographyMatch(
                screen, template, screenRect, templateRegion,
                out score, out location, cfg, callerName);
        }

        /// <summary>
        /// AKAZE + BFMatcher(KNN) + RANSACホモグラフィでロバストに一致判定。
        /// score=インライア率、location=推定左上座標（画面全体基準）。
        /// </summary>
        public static bool TryOrbHomographyMatch(
            Mat screen, Mat template,
            Rectangle screenRect, RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            // ORB 検出器を生成（cfg を反映）
            Func<Feature2D> create = () => ORB.Create(
                nFeatures: cfg?.NFeatures ?? 1000,
                scaleFactor: cfg?.ScaleFactor ?? 1.2f,
                nLevels: cfg?.NLevels ?? 8,
                edgeThreshold: cfg?.EdgeThreshold ?? 31,
                firstLevel: cfg?.FirstLevel ?? 0,
                wtaK: cfg?.WtaK ?? 2,
                scoreType: cfg?.ScoreType ?? ORBScoreType.Harris,
                patchSize: cfg?.PatchSize ?? 31,
                fastThreshold: cfg?.FastThreshold ?? 20);

            return TryFeatureHomographyMatch(
                screen, template, screenRect, templateRegion,
                create, NormTypes.Hamming,
                out score, out location, cfg, callerName);
        }

        // ----------------- AKAZE -----------------
        public static bool TryAkazeHomographyMatch(
            Mat screen, Mat template,
            RelativeRegion screenRegion, RelativeRegion.Anchor2D screenAnchor, RelativeRegion.ScaleMode screenScale,
            RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            var screenRect = screenRegion.ComputeRect(screen.Width, screen.Height, screenAnchor, screenScale);
            return TryAkazeHomographyMatch(screen, template, screenRect, templateRegion, out score, out location, cfg, callerName);
        }
        public static bool TryAkazeHomographyMatch(
            Mat screen, Mat template,
            Rectangle screenRect, RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            Func<Feature2D> create = () => AKAZE.Create(
                descriptorType: AKAZEDescriptorType.MLDB,
                descriptorSize: 0, descriptorChannels: 3,
                threshold: 0.001f, nOctaves: 4, nOctaveLayers: 4,
                diffusivity: KAZEDiffusivityType.DiffPmG2);
            return TryFeatureHomographyMatch(
                screen, template, screenRect, templateRegion,
                create, NormTypes.Hamming,
                out score, out location, cfg, callerName);
        }

        // ----------------- KAZE -----------------
        public static bool TryKazeHomographyMatch(
            Mat screen, Mat template,
            RelativeRegion screenRegion, RelativeRegion.Anchor2D screenAnchor, RelativeRegion.ScaleMode screenScale,
            RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            var screenRect = screenRegion.ComputeRect(screen.Width, screen.Height, screenAnchor, screenScale);
            return TryKazeHomographyMatch(screen, template, screenRect, templateRegion, out score, out location, cfg, callerName);
        }
        public static bool TryKazeHomographyMatch(
            Mat screen, Mat template,
            Rectangle screenRect, RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            Func<Feature2D> create = () => KAZE.Create(
                extended: false, upright: false, threshold: 0.001f,
                nOctaves: 4, nOctaveLayers: 4, diffusivity: KAZEDiffusivityType.DiffPmG2);
            return TryFeatureHomographyMatch(
                screen, template, screenRect, templateRegion,
                create, NormTypes.L2,
                out score, out location, cfg, callerName);
        }

        // ----------------- SIFT -----------------
        public static bool TrySiftHomographyMatch(
            Mat screen, Mat template,
            RelativeRegion screenRegion, RelativeRegion.Anchor2D screenAnchor, RelativeRegion.ScaleMode screenScale,
            RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            var screenRect = screenRegion.ComputeRect(screen.Width, screen.Height, screenAnchor, screenScale);
            return TrySiftHomographyMatch(screen, template, screenRect, templateRegion, out score, out location, cfg, callerName);
        }
        public static bool TrySiftHomographyMatch(
            Mat screen, Mat template,
            Rectangle screenRect, RelativeRegion templateRegion,
            out double score, out OpenCvSharp.Point location, OrbMatchConfig? cfg = null,
            [CallerMemberName] string? callerName = null)
        {
            // 明るい領域でも特徴を拾いやすいように、contrastThreshold を下げる
            // 既定 ≈ 0.04 → 0.01（必要なら 0.005 などさらに下げても良い）
            Func<Feature2D> create = () => SIFT.Create(
                nFeatures: 0,
                nOctaveLayers: 3,
                contrastThreshold: 0.01,
                edgeThreshold: 10,
                sigma: 1.6);
            return TryFeatureHomographyMatch(
                screen, template, screenRect, templateRegion,
                create, NormTypes.L2,
                out score, out location, cfg, callerName);
        }

        public static bool TryChamferMatch(
            Mat screen, Mat template, System.Drawing.Rectangle screenRect,
            RelativeRegion tplRR,  // 使わなくてもOK（将来マスク対応用に受けておく）
            out double score, out OpenCvSharp.Point bestLoc,
            double canny1 = 70, double canny2 = 200,
            double[]? scales = null,  // 例: new[]{0.9,1.0,1.1}
            ChamferDebug? dbg = null
        )
        {
            score = 0; bestLoc = default;
            if (screen.Empty() || template.Empty() || screenRect.Width <= 0 || screenRect.Height <= 0) return false;

            scales ??= new[] { 0.95, 1.0, 1.05 }; // まずは等倍だけ。必要なら 0.9/1.1 も追加

            using var roi = new Mat(screen, new OpenCvSharp.Rect(screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height));
            using var roiGray = new Mat();
            // 入力のチャンネル数に応じて安全に GRAY 化
            int scn = roi.Channels();
            if (scn == 1)
            {
                roi.CopyTo(roiGray);
            }
            else if (scn == 3)
            {
                Cv2.CvtColor(roi, roiGray, ColorConversionCodes.BGR2GRAY);
            }
            else if (scn == 4)
            {
                Cv2.CvtColor(roi, roiGray, ColorConversionCodes.BGRA2GRAY);
            }
            else
            {
#if DEBUG
                Console.WriteLine($"[Chamfer] Unexpected channel count: {scn}, fallback CopyTo");
#endif
                roi.CopyTo(roiGray);
            }
#if DEBUG
            //DumpIf(dbg, "roi", Ensure8U(roi));
            //DumpIf(dbg, "roiGray", Ensure8U(roiGray));
#endif
            using var roiEdge = new Mat();
            Cv2.Canny(roiGray, roiEdge, canny1, canny2);
#if DEBUG
            //DumpIf(dbg, "roiEdge", Ensure8U(roiEdge));
#endif
            // Distance Transform（エッジ=0, 背景=1）
            using var inv = new Mat();
            Cv2.Threshold(roiEdge, inv, 0, 255, ThresholdTypes.BinaryInv);
            if (inv.Type() != MatType.CV_8UC1) inv.ConvertTo(inv, MatType.CV_8U);
#if DEBUG
            //DumpIf(dbg, "inv", Ensure8U(inv));
#endif
            using var dt = new Mat();
            Cv2.DistanceTransform(inv, dt, DistanceTypes.L2, DistanceTransformMasks.Mask3);
            Cv2.Normalize(dt, dt, 0, 1, NormTypes.MinMax); // 0..1 に正規化
#if DEBUG
            //DumpIf(dbg, "dt_gray", ToVis8U(dt));
            //DumpColorMapIf(dbg, "dt_heat", ToVis8U(dt));
#endif
            double best = double.PositiveInfinity;
            OpenCvSharp.Point bestP = default;
#if DEBUG
            OpenCvSharp.Point bestRespLoc = default;
            OpenCvSharp.Size bestTplSize = default;
#endif
            foreach (var s in scales)
            {
                // テンプレのエッジマスク（0/1, CV_32F）
                using var tResized = (Math.Abs(s - 1.0) < 1e-6)
                    ? template.Clone() // 入力Matを誤ってDisposeしないためクローン
                    : template.Resize(new OpenCvSharp.Size(Math.Max(1, (int)Math.Round(template.Width * s)),
                                                        Math.Max(1, (int)Math.Round(template.Height * s))));
                using var tGray = new Mat();
                if (tResized.Channels() == 1)
                    tResized.CopyTo(tGray); // 再代入禁止 → 中身をコピー
                else
                    Cv2.CvtColor(tResized, tGray, ColorConversionCodes.BGR2GRAY);
                using var tEdge = new Mat();
                Cv2.Canny(tGray, tEdge, canny1, canny2);

                // カーネル作成（エッジ=1, 他=0）→ 正規化
                using var kernel = new Mat();
                tEdge.ConvertTo(kernel, MatType.CV_32F, 1.0 / 255.0);
                var sum = Cv2.Sum(kernel).Val0;
                if (sum <= 0.0) continue;
                Cv2.Divide(kernel, sum, kernel); // 再代入禁止 → インプレース正規化

                // 距離画像 dt と畳み込み → 各位置の平均距離マップ resp
                using var resp = new Mat();
                Cv2.Filter2D(dt, resp, MatType.CV_32F, kernel, new OpenCvSharp.Point(-1, -1), 0, BorderTypes.Constant);

                // 小さいほど良い（距離が小さい）
                Cv2.MinMaxLoc(resp, out double minVal, out _, out OpenCvSharp.Point minLoc /*center*/, out _);
                if (minVal < best)
                {
                    best = minVal;
#if DEBUG
                    bestRespLoc = minLoc;
                    bestTplSize = new OpenCvSharp.Size(tResized.Width, tResized.Height);
                    // ベストのレスポンスを保存（可視化）
                    DumpIf(dbg, "resp_best_gray", ToVis8U(resp));
                    DumpColorMapIf(dbg, "resp_best_heat", ToVis8U(resp));
#endif
                }
            }

            if (double.IsPositiveInfinity(best)) return false;

            // 基本スコア: 0..1 の“類似度”（1が良い）
            double scoreChamfer = 1.0 - Math.Min(1.0, Math.Max(0.0, best));
            // カーネル中心 → 左上へ補正
            int tlx = bestRespLoc.X - (bestTplSize.Width  / 2);
            int tly = bestRespLoc.Y - (bestTplSize.Height / 2);
            // 画面全体座標の左上
            bestP = new OpenCvSharp.Point(screenRect.X + tlx, screenRect.Y + tly);
            bestLoc = bestP;

            // === 追加: 形状の相互カバレッジ & エッジ密度ペナルティ ===
            // best となった倍率・位置でテンプレエッジとROIエッジの一致具合を測る
            double bestScale = (bestTplSize.Width > 0) ? (bestTplSize.Width / (double)template.Width) : 1.0;
            using var tRes2 = (Math.Abs(bestScale - 1.0) < 1e-6)
                ? template.Clone()
                : template.Resize(new OpenCvSharp.Size(bestTplSize.Width, bestTplSize.Height));
            using var tGray2 = new Mat();
            if (tRes2.Channels() == 1) tRes2.CopyTo(tGray2);
            else Cv2.CvtColor(tRes2, tGray2, ColorConversionCodes.BGR2GRAY);
            using var tEdge2 = new Mat();
            Cv2.Canny(tGray2, tEdge2, canny1, canny2);

            var win = new OpenCvSharp.Rect(tlx, tly, tRes2.Width, tRes2.Height);
            // best位置のテンプレサイズでROIから切り出す（境界クランプ）
            var winClamped = ClampRectToBounds(win, roiEdge.Width, roiEdge.Height);
            if (winClamped.Width <= 0 || winClamped.Height <= 0)
            {
#if DEBUG
                Console.WriteLine($"[Chamfer] reject: window out of bounds win={win} roi=({roiEdge.Width}x{roiEdge.Height})");
#endif
                score = 0; return false;
            }
            using var roiEdgeWin = new Mat(roiEdge, winClamped);
            // 3x3 膨張で位置ズレを許容
            using var k3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            // --- 追加: 共通サイズにクロップ ---
            int ow = Math.Min(tEdge2.Width, roiEdgeWin.Width);
            int oh = Math.Min(tEdge2.Height, roiEdgeWin.Height);
            if (ow <= 0 || oh <= 0)
            {
#if DEBUG
                Console.WriteLine($"[Chamfer] reject: overlap size invalid ow={ow}, oh={oh}");
#endif
                score = 0; return false;
            }
            using var tEdge2C     = new Mat(tEdge2,     new OpenCvSharp.Rect(0, 0, ow, oh));
            using var roiEdgeWinC = new Mat(roiEdgeWin, new OpenCvSharp.Rect(0, 0, ow, oh));
            // 膨張はクロップ後に実施（サイズ/型を揃えたまま）
            using var tplDil = new Mat(); Cv2.Dilate(tEdge2C,     tplDil, k3);
            using var roiDil = new Mat(); Cv2.Dilate(roiEdgeWinC, roiDil, k3);

            double sumTpl = Cv2.Sum(tEdge2C).Val0 / 255.0;
            double sumRoi = Cv2.Sum(roiEdgeWinC).Val0 / 255.0;
            double covTpl = 0, covRoi = 0;
            if (sumTpl > 0 && sumRoi > 0)
            {
                using var and1 = new Mat(); Cv2.BitwiseAnd(tEdge2C, roiDil, and1);
                using var and2 = new Mat(); Cv2.BitwiseAnd(roiEdgeWinC, tplDil, and2);
                covTpl = (Cv2.Sum(and1).Val0 / 255.0) / Math.Max(1.0, sumTpl); // 「テンプレのエッジの何割がROIに存在するか」
                covRoi = (Cv2.Sum(and2).Val0 / 255.0) / Math.Max(1.0, sumRoi); // 「ROIのエッジの何割がテンプレに対応するか」
            }
            double overlap = Math.Min(covTpl, covRoi); // 双方向の小さい方を採用

            // ROIウィンドウのエッジ密度（多すぎると誤検出増）
            double area = Math.Max(1, ow * oh);
            double edgeDensity = sumRoi / area; // 0..1（近似）
            // 閾値より高い密度に線形ペナルティ（例: 18% 超過分を強く減点）
            const double refD = 0.18; const double slope = 2.5;
            double densityPenalty = (edgeDensity > refD) ? Math.Max(0.0, 1.0 - (edgeDensity - refD) * slope) : 1.0;

            // オーバーラップが低すぎる場合は即不一致（Chamferだけの過大評価を抑制）
            const double overlapGate = 0.20;
            if (overlap < overlapGate)
            {
#if DEBUG
                Console.WriteLine($"[Chamfer] reject by overlap={overlap:F3}, density={edgeDensity:F3}, base={scoreChamfer:F3}");
#endif
                score = overlap * 0.1; // ログ観察用に極小値を返して false
                return false;
            }

            // 最終スコア: Chamfer × オーバーラップ × 密度ペナルティ（すべて 0..1）
            score = scoreChamfer * overlap * densityPenalty;

#if DEBUG
            // ROI上に検出枠を描いたオーバレイ
            if (dbg != null && bestTplSize.Width > 0 && bestTplSize.Height > 0)
            {
                using var overlay = new Mat();
                if (roi.Channels() == 1) Cv2.CvtColor(roi, overlay, ColorConversionCodes.GRAY2BGR);
                else roi.CopyTo(overlay);
                var rect = winClamped;
                Cv2.Rectangle(overlay, rect, new Scalar(0, 255, 0), 2);
                Cv2.PutText(overlay, $"ov={overlap:F2} dens={edgeDensity:F2} base={scoreChamfer:F2} fin={score:F2}",
                            new OpenCvSharp.Point(rect.X+2, rect.Y+16), HersheyFonts.HersheySimplex, 0.5, new Scalar(0,255,0), 1);
                DumpIf(dbg, "overlay", Ensure8U(overlay));
            }
#endif
            return true;
        }

        /// <summary>Chamferマッチのデバッグ保存設定</summary>
        public sealed class ChamferDebug
        {
            public string Dir { get; }
            public string Tag { get; }   // ファイル名の接頭辞
            public ChamferDebug(string dir, string tag)
            {
                Dir = dir ?? ".";
                Tag = string.IsNullOrWhiteSpace(tag) ? "dbg" : tag;
            }
        }
    
        // ROI境界に矩形をクランプ（負も含めて安全化）
        private static OpenCvSharp.Rect ClampRectToBounds(OpenCvSharp.Rect r, int bw, int bh)
        {
            int x = Math.Max(0, Math.Min(r.X, bw - 1));
            int y = Math.Max(0, Math.Min(r.Y, bh - 1));
            int w = Math.Min(r.Width,  bw - x);
            int h = Math.Min(r.Height, bh - y);
            w = Math.Max(0, w); h = Math.Max(0, h);
            return new OpenCvSharp.Rect(x, y, w, h);
        }

            // ---- デバッグ保存ヘルパ ----
        private static void DumpIf(ChamferDebug? dbg, string name, Mat m)
        {
            if (dbg == null || m.Empty()) return;
            Directory.CreateDirectory(dbg.Dir);
            var path = Path.Combine(dbg.Dir, $"{dbg.Tag}_{name}.png");
            Cv2.ImWrite(path, m);
        }
        private static void DumpColorMapIf(ChamferDebug? dbg, string name, Mat gray8u)
        {
            if (dbg == null || gray8u.Empty()) return;
            using var color = new Mat();
            Cv2.ApplyColorMap(gray8u, color, ColormapTypes.Jet);
            DumpIf(dbg, name, color);
        }
        private static Mat ToVis8U(Mat m)  // 32F(0..1)などを8Uへ
        {
            if (m.Empty()) return m;
            if (m.Type() == MatType.CV_8UC1) return m.Clone();
            using var norm = new Mat();
            Cv2.Normalize(m, norm, 0, 255, NormTypes.MinMax);
            var u8 = new Mat();
            norm.ConvertTo(u8, MatType.CV_8U);
            return u8;
        }
        private static Mat Ensure8U(Mat m)
        {
            if (m.Empty()) return m;
            if (m.Type() == MatType.CV_8UC1 || m.Type() == MatType.CV_8UC3 || m.Type() == MatType.CV_8UC4) return m.Clone();
            return ToVis8U(m);
        }
    }
}