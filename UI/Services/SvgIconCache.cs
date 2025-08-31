using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DuelLedger.UI.Services;

public sealed class SvgIconCache
{
    private readonly string _root;
    private readonly HttpClient _http;
    private readonly bool _allowRemote = true;
    public event Action<string, string>? IconReady;

    public SvgIconCache()
    {
        _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DuelLedger", "icons");
        Directory.CreateDirectory(_root);
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    // UI thread only: returns local path if exists without any I/O.
    public string? TryGetLocalOnly(string key)
    {
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var filePath = Path.Combine(_root, safeKey + ".svg");
        return File.Exists(filePath) ? filePath : null;
    }

    // Background: fetch icon and save locally. On success triggers IconReady.
    public async Task FetchAsync(string key, string iconUrl, CancellationToken ct = default)
    {
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var filePath = Path.Combine(_root, safeKey + ".svg");
        var etagPath = filePath + ".etag";
        var modPath = filePath + ".mod";
        try
        {
            if (!_allowRemote) return;
            using var req = new HttpRequestMessage(HttpMethod.Get, iconUrl);
            if (File.Exists(etagPath))
                req.Headers.TryAddWithoutValidation("If-None-Match", await File.ReadAllTextAsync(etagPath, ct));
            if (File.Exists(modPath))
                req.Headers.TryAddWithoutValidation("If-Modified-Since", await File.ReadAllTextAsync(modPath, ct));
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.StatusCode == HttpStatusCode.NotModified && File.Exists(filePath))
            {
                IconReady?.Invoke(key, filePath);
                return;
            }
            if (resp.IsSuccessStatusCode)
            {
                var data = await resp.Content.ReadAsByteArrayAsync(ct);
                await File.WriteAllBytesAsync(filePath, data, ct);
                if (resp.Headers.ETag is not null)
                    await File.WriteAllTextAsync(etagPath, resp.Headers.ETag.ToString(), ct);
                if (resp.Content.Headers.LastModified.HasValue)
                    await File.WriteAllTextAsync(modPath, resp.Content.Headers.LastModified.Value.ToString("R"), ct);
                Console.WriteLine($"SvgIconCache: fetched {iconUrl}");
                IconReady?.Invoke(key, filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SvgIconCache.FetchAsync: {ex.Message}");
        }
    }

    public string? GetLocalPath(string key, string? iconUrl)
    {
        if (string.IsNullOrWhiteSpace(iconUrl)) return null;
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var filePath = Path.Combine(_root, safeKey + ".svg");
        var etagPath = filePath + ".etag";
        var modPath = filePath + ".mod";

        try
        {
            if (File.Exists(filePath))
            {
                if (!_allowRemote) return filePath;
            }

            if (!_allowRemote && !File.Exists(filePath)) return null;

            var req = new HttpRequestMessage(HttpMethod.Get, iconUrl);
            if (File.Exists(etagPath))
                req.Headers.TryAddWithoutValidation("If-None-Match", File.ReadAllText(etagPath));
            if (File.Exists(modPath))
                req.Headers.TryAddWithoutValidation("If-Modified-Since", File.ReadAllText(modPath));

            var resp = _http.Send(req);
            if (resp.StatusCode == HttpStatusCode.NotModified && File.Exists(filePath))
                return filePath;

            if (resp.IsSuccessStatusCode)
            {
                var data = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                File.WriteAllBytes(filePath, data);
                if (resp.Headers.ETag != null)
                    File.WriteAllText(etagPath, resp.Headers.ETag.ToString());
                if (resp.Content.Headers.LastModified.HasValue)
                    File.WriteAllText(modPath, resp.Content.Headers.LastModified.Value.ToString("R"));
                Console.WriteLine($"SvgIconCache: fetched {iconUrl}");
                return filePath;
            }

            if (File.Exists(filePath)) return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SvgIconCache: {ex.Message}");
            if (File.Exists(filePath)) return filePath;
        }

        return null;
    }
}
