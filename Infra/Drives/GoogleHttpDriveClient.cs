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
    private static readonly HttpClient Http = new();
    private readonly string _baseUrl;
    private readonly string _manifest;

    public GoogleHttpDriveClient(string baseUrl, string manifest)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _manifest = manifest;
    }

    public async Task<IReadOnlyList<RemoteFile>> GetManifestAsync(CancellationToken ct)
    {
        var url = $"{_baseUrl}/{_manifest}";
        using var response = await Http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = await JsonSerializer.DeserializeAsync<List<RemoteFile>>(stream, options, ct);
        return list ?? new List<RemoteFile>();
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken ct)
    {
        var url = $"{_baseUrl}/{path}";
        return await Http.GetStreamAsync(url, ct);
    }
}
