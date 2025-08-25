using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace DuelLedger.UI.Services;

public sealed class SvgIconCache
{
    private readonly string _root;
    private readonly HttpClient _http;
    private readonly bool _allowRemote = true;

    public SvgIconCache()
    {
        _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DuelLedger", "icons");
        Directory.CreateDirectory(_root);
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
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
