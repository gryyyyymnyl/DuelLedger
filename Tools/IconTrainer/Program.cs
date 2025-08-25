using DuelLedger.Net;
using DuelLedger.Vision;
using SkiaSharp;
using Svg.Skia;
using OpenCvSharp;
using System.Text.Json;

if (args.Length == 0)
{
    Console.WriteLine("Usage: IconTrainer --out <dir> --per-class <N>");
    return;
}
string outDir = "models";
int perClass = 100;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--out" && i + 1 < args.Length) outDir = args[++i];
    if (args[i] == "--per-class" && i + 1 < args.Length) perClass = int.Parse(args[++i]);
}
Directory.CreateDirectory(outDir);
var provider = new CachedSvgProvider(new HttpSvgProvider());
await Trainer.TrainAsync(provider, outDir, perClass);

public static class Trainer
{
    public static async Task TrainAsync(ISvgProvider provider, string outDir, int perClass)
    {
        var feats = new Dictionary<int, List<float[]>>();
        for (int id = 1; id <= 7; id++)
        {
            var svg = await provider.GetSvgAsync(id);
            if (svg == null) continue;
            feats[id] = new();
            for (int i = 0; i < perClass; i++)
            {
                using var bmp = Rasterize(svg, 128, 128);
                using var mat = BitmapToMat(bmp);
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2GRAY);
                feats[id].Add(HogFeaturizer.Extract(mat));
            }
        }
        var centroids = new Dictionary<int, float[]>();
        foreach (var kv in feats)
        {
            var len = kv.Value[0].Length;
            var mean = new float[len];
            foreach (var f in kv.Value)
                for (int i = 0; i < len; i++) mean[i] += f[i];
            for (int i = 0; i < len; i++) mean[i] /= kv.Value.Count;
            centroids[kv.Key] = mean;
        }
        var json = JsonSerializer.Serialize(centroids);
        await File.WriteAllTextAsync(Path.Combine(outDir, "class_icon_svm.json"), json);
        await File.WriteAllTextAsync(Path.Combine(outDir, "class_icon_platt.json"), "{}");
    }

    private static SKBitmap Rasterize(string svg, int w, int h)
    {
        var svgDom = new SKSvg();
        svgDom.FromSvg(svg);
        var bmp = new SKBitmap(w, h, true);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        var scale = Math.Min(w / svgDom.Picture.CullRect.Width, h / svgDom.Picture.CullRect.Height);
        canvas.Scale(scale, scale);
        canvas.DrawPicture(svgDom.Picture);
        return bmp;
    }

    private static Mat BitmapToMat(SKBitmap bmp)
    {
        var info = bmp.Info;
        return Mat.FromPixelData(info.Height, info.Width, MatType.CV_8UC4, bmp.GetPixels());
    }
}
