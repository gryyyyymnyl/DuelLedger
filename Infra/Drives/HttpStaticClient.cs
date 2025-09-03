using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core.Drives;

namespace DuelLedger.Infra.Drives;

public sealed class HttpStaticClient : IRemoteDriveClient
{
    private readonly HttpClient _http;
    private readonly RemoteConfig _config;

    public HttpStaticClient(IAppConfig config)
    {
        _config = config.Value.Assets.Remote ?? new RemoteConfig();
        _http = new HttpClient
        {
            BaseAddress = new Uri(_config.BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<IReadOnlyList<RemoteEntry>> GetManifestAsync(CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var resp = await _http.GetAsync(_config.Manifest, ct);
                if (!resp.IsSuccessStatusCode)
                    break;
                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var list = await JsonSerializer.DeserializeAsync<List<RemoteEntry>>(stream, opts, ct);
                return list ?? new List<RemoteEntry>();
            }
            catch (Exception ex) when (attempt < 2)
            {
                Console.WriteLine($"HttpStaticClient.GetManifestAsync retry {attempt + 1}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpStaticClient.GetManifestAsync failed: {ex.Message}");
                break;
            }
        }
        return Array.Empty<RemoteEntry>();
    }

    public async Task<Stream?> DownloadAsync(string path, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var resp = await _http.GetAsync(path, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"HttpStaticClient.DownloadAsync status {resp.StatusCode} for {path}");
                    return null;
                }
                var ms = new MemoryStream();
                await resp.Content.CopyToAsync(ms, ct);
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex) when (attempt < 2)
            {
                Console.WriteLine($"HttpStaticClient.DownloadAsync retry {attempt + 1} for {path}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpStaticClient.DownloadAsync failed for {path}: {ex.Message}");
                break;
            }
        }
        return null;
    }
}
