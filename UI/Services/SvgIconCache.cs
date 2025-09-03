using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DuelLedger.UI.Services;

/// <summary>
/// Asynchronously fetches and caches SVG icons on disk.
/// Subsequent requests return the cached path immediately.
/// </summary>
public sealed class SvgIconCache
{
    public static SvgIconCache Instance { get; } = new();

    private readonly string _root;
    private readonly HttpClient _http;
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _pending = new();

    public event Action<string, string>? IconReady;

    public SvgIconCache(string? root = null, HttpClient? http = null)
    {
        _root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DuelLedger", "icons");
        Directory.CreateDirectory(_root);
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    /// <summary>
    /// Returns a local path if cached; otherwise begins an async fetch and returns null.
    /// </summary>
    public string? Get(string key, string? iconUrl)
    {
        if (string.IsNullOrWhiteSpace(iconUrl)) return null;

        var safeKey = SafeKey(key);
        var filePath = Path.Combine(_root, safeKey + ".svg");
        if (File.Exists(filePath)) return filePath;

        var lazy = _pending.GetOrAdd(safeKey, _ => new Lazy<Task<string?>>(() => FetchAsync(safeKey, iconUrl)));
        _ = lazy.Value.ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion && t.Result is not null)
                IconReady?.Invoke(key, t.Result);
            _pending.TryRemove(safeKey, out _);
        });
        return null;
    }

    private async Task<string?> FetchAsync(string safeKey, string iconUrl)
    {
        var filePath = Path.Combine(_root, safeKey + ".svg");
        var etagPath = filePath + ".etag";
        var modPath = filePath + ".mod";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, iconUrl);
            if (File.Exists(etagPath))
                req.Headers.TryAddWithoutValidation("If-None-Match", await File.ReadAllTextAsync(etagPath));
            if (File.Exists(modPath))
                req.Headers.TryAddWithoutValidation("If-Modified-Since", await File.ReadAllTextAsync(modPath));
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (resp.StatusCode == HttpStatusCode.NotModified && File.Exists(filePath))
                return filePath;
            if (resp.IsSuccessStatusCode)
            {
                var data = await resp.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(filePath, data);
                if (resp.Headers.ETag is not null)
                    await File.WriteAllTextAsync(etagPath, resp.Headers.ETag.ToString());
                if (resp.Content.Headers.LastModified.HasValue)
                    await File.WriteAllTextAsync(modPath, resp.Content.Headers.LastModified.Value.ToString("R"));
                return filePath;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SvgIconCache.FetchAsync: {ex.Message}");
        }
        return null;
    }

    private static string SafeKey(string key)
        => string.Join("_", key.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
}

