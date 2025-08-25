using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using OpenCvSharp;

namespace DuelLedger.Vision;

public class SvgTemplate
{
    public required Mat Binary { get; init; }
    public required Mat Dist { get; init; }
    public required Mat Gray { get; init; }
}

/// <summary>SVGテンプレートのキャッシュ管理</summary>
public static class SvgTemplateCache
{
    private static readonly Dictionary<(int id, int w, int h, double rot), SvgTemplate> _cache = new();

    private static readonly Dictionary<int, string> _urls = new()
    {
        {1, "https://shadowverse-wb.com/assets/images/common/common/class/class_elf.svg"},
        {2, "https://shadowverse-wb.com/assets/images/common/common/class/class_royal.svg"},
        {3, "https://shadowverse-wb.com/assets/images/common/common/class/class_witch.svg"},
        {4, "https://shadowverse-wb.com/assets/images/common/common/class/class_dragon.svg"},
        {5, "https://shadowverse-wb.com/assets/images/common/common/class/class_nightmare.svg"},
        {6, "https://shadowverse-wb.com/assets/images/common/common/class/class_bishop.svg"},
        {7, "https://shadowverse-wb.com/assets/images/common/common/class/class_nemesis.svg"},
    };

    public static SvgTemplate Get(int classId, int w, int h, double rot = 0)
    {
        var key = (classId, w, h, rot);
        if (_cache.TryGetValue(key, out var tpl)) return tpl;

        var svgXml = LoadSvgXml(classId);
        if (string.IsNullOrWhiteSpace(svgXml))
        {
            tpl = new SvgTemplate { Binary = new Mat(), Dist = new Mat(), Gray = new Mat() };
            _cache[key] = tpl;
            return tpl;
        }

        var (gray, binary) = ImageMatch.RenderSvgToMat(svgXml, w, h);

        Mat grayR = gray, binR = binary;
        if (Math.Abs(rot) > 0.001)
        {
            var center = new Point2f(gray.Width / 2f, gray.Height / 2f);
            using var m = Cv2.GetRotationMatrix2D(center, rot, 1.0);
            var g2 = new Mat();
            Cv2.WarpAffine(gray, g2, m, gray.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));
            var b2 = new Mat();
            Cv2.WarpAffine(binary, b2, m, binary.Size(), InterpolationFlags.Nearest, BorderTypes.Constant, Scalar.All(0));
            grayR = g2; binR = b2;
        }

        var dist = ImageMatch.BuildDistanceTemplate(binR);
        tpl = new SvgTemplate
        {
            Binary = binR.Clone(),
            Dist = dist.Clone(),
            Gray = grayR.Clone()
        };
        _cache[key] = tpl;
        return tpl;
    }

    private static string? LoadSvgXml(int classId)
    {
        if (!_urls.TryGetValue(classId, out var url)) return null;

        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SWBT", "svgcache");
        Directory.CreateDirectory(cacheDir);
        var fileName = Path.GetFileName(url);
        var cachePath = Path.Combine(cacheDir, fileName);
        try
        {
            if (!File.Exists(cachePath))
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(cachePath, data);
            }
            return File.ReadAllText(cachePath);
        }
        catch (Exception ex)
        {
            if (File.Exists(cachePath))
            {
                try { return File.ReadAllText(cachePath); } catch { }
            }
            Console.WriteLine($"[SvgTemplate] WARN: {ex.Message}");
            return null;
        }
    }
}

