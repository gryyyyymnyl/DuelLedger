using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Svg.Skia;
using SkiaSharp;

namespace DuelLedger.UI.Services;

public sealed class SvgIconCache
{
    private readonly ConcurrentDictionary<string, Bitmap> _memory = new();
    private readonly string _root;

    private SvgIconCache()
    {
        _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DuelLedger", "IconCache");
        Directory.CreateDirectory(_root);
    }

    private static readonly Lazy<SvgIconCache> _lazy = new(() => new SvgIconCache());
    public static SvgIconCache Instance => _lazy.Value;

    public event EventHandler<string>? IconReady;

    public Bitmap? TryGet(string key)
        => _memory.TryGetValue(key, out var bmp) ? bmp : null;

    private static readonly byte[] _emptyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAAWgmWQ0AAAAASUVORK5CYII=");
    public Bitmap Placeholder { get; } = new(new MemoryStream(_emptyPng));

    public async Task<Bitmap> GetOrCreateAsync(string key, Uri svgPath, Size size, CancellationToken ct)
    {
        if (_memory.TryGetValue(key, out var ready))
            return ready;

        var file = Path.Combine(_root, key + ".png");
        if (File.Exists(file))
        {
            try
            {
                await using var fs = File.OpenRead(file);
                var bmp = await Task.Run(() => Bitmap.DecodeToWidth(fs, (int)size.Width), ct).ConfigureAwait(false);
                _memory[key] = bmp;
                IconReady?.Invoke(this, key);
                return bmp;
            }
            catch { /* fallthrough */ }
        }

        try
        {
            var bmp = await Task.Run(() => RenderSvg(svgPath, size, ct), ct).ConfigureAwait(false);
            await using (var fs = File.Open(file, FileMode.Create, FileAccess.Write))
            {
                bmp.Save(fs);
            }
            _memory[key] = bmp;
            IconReady?.Invoke(this, key);
            return bmp;
        }
        catch
        {
            try { if (File.Exists(file)) File.Delete(file); } catch { }
            return Placeholder;
        }
    }

    public void Warmup(IEnumerable<(string key, Uri path, Size size)> items)
    {
        foreach (var item in items)
        {
            _ = GetOrCreateAsync(item.key, item.path, item.size, CancellationToken.None);
        }
    }

    private static Bitmap RenderSvg(Uri path, Size size, CancellationToken ct)
    {
        using var stream = AssetLoader.Open(path);
        var svg = new SKSvg();
        svg.Load(stream);
        var pic = svg.Picture;
        if (pic == null)
            throw new InvalidOperationException("SVG picture not loaded");
        var bounds = pic.CullRect;
        var scaleX = (float)(size.Width / bounds.Width);
        var scaleY = (float)(size.Height / bounds.Height);
        var info = new SKImageInfo((int)size.Width, (int)size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scaleX, scaleY);
        canvas.DrawPicture(pic);
        canvas.Flush();
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        ct.ThrowIfCancellationRequested();
        return new Bitmap(ms);
    }
}

