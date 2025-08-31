using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core.Templates;

namespace DuelLedger.Infra.Drives;

public sealed class GoogleHttpDriveClient : IRemoteDriveClient
{
    private static readonly HttpClient Http = CreateClient();
    private readonly string _baseUrl;
    private readonly string _manifest;

    public GoogleHttpDriveClient(string baseUrl, string manifest)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _manifest = manifest;
    }

    private static HttpClient CreateClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private static async Task<T> Retry<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        for (var i = 0; i < 2; i++)
        {
            try
            {
                return await action(ct);
            }
            catch when (i < 1)
            {
                await Task.Delay(500, ct);
            }
        }
        return await action(ct);
    }

    public Task<IReadOnlyList<RemoteFile>> GetManifestAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/{_manifest}";
        return Retry(async token =>
        {
            using var response = await Http.GetAsync(url, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = await JsonSerializer.DeserializeAsync<List<RemoteFile>>(stream, options, token);
            return (IReadOnlyList<RemoteFile>)(list ?? new List<RemoteFile>());
        }, ct);
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken ct)
    {
        var url = $"{_baseUrl}/{path}";
        return Retry(token => Http.GetStreamAsync(url, token), ct);
    }
}
