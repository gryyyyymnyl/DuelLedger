using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core.Drives;
using DuelLedger.Core.Templates;

namespace DuelLedger.Infra.Templates;

public sealed class TemplateSyncService
{
    private readonly IAppConfig _config;
    private readonly ITemplatePathResolver _resolver;
    private readonly IRemoteDriveClient _client;

    public TemplateSyncService(IAppConfig config, ITemplatePathResolver resolver, IRemoteDriveClient client)
    {
        _config = config;
        _resolver = resolver;
        _client = client;
    }

    public async Task SyncAsync(string gameName, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (_config.Value.Assets.Remote is null)
            return;
        var root = _resolver.Get(gameName);
        Directory.CreateDirectory(root);

        // Download manifest.json and compare with local copy
        var manifestStream = await _client.DownloadAsync(_config.Value.Assets.Remote.Manifest, ct);
        if (manifestStream == null)
            return;
        using var ms = new MemoryStream();
        await manifestStream.CopyToAsync(ms, ct);
        var manifestBytes = ms.ToArray();

        var manifestPath = Path.Combine(root, "manifest.json");
        if (File.Exists(manifestPath))
        {
            var localBytes = await File.ReadAllBytesAsync(manifestPath, ct);
            if (localBytes.SequenceEqual(manifestBytes))
            {
                Console.WriteLine("[Sync] manifest up-to-date");
                return;
            }
        }

        await File.WriteAllBytesAsync(manifestPath, manifestBytes, ct);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var manifest = JsonSerializer.Deserialize<List<RemoteEntry>>(manifestBytes, options) ?? new List<RemoteEntry>();
        var extensions = new HashSet<string>(_config.Value.Assets.Remote.Extensions ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

        int total = manifest.Count, updated = 0, skipped = 0, failed = 0, processed = 0;
        progress?.Report(0);

        foreach (var entry in manifest)
        {
            processed++;
            var ext = Path.GetExtension(entry.Path);
            if (extensions.Count > 0 && !extensions.Contains(ext))
            {
                Console.WriteLine($"[Sync] skip {entry.Path} (unsupported extension)");
                skipped++;
                progress?.Report((double)processed / total);
                continue;
            }

            var localPath = Path.Combine(root, entry.Path);
            try
            {
                bool needDownload = true;
                if (File.Exists(localPath))
                {
                    if (!string.IsNullOrEmpty(entry.Sha256))
                    {
                        using var fs = File.OpenRead(localPath);
                        using var sha = SHA256.Create();
                        var hash = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
                        if (string.Equals(hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[Sync] up-to-date {entry.Path} (sha256)");
                            skipped++;
                            needDownload = false;
                        }
                    }
                    else if (entry.LastModifiedUtc.HasValue)
                    {
                        var lastWrite = File.GetLastWriteTimeUtc(localPath);
                        if (lastWrite >= entry.LastModifiedUtc.Value)
                        {
                            Console.WriteLine($"[Sync] up-to-date {entry.Path} (timestamp)");
                            skipped++;
                            needDownload = false;
                        }
                    }
                }

                if (!needDownload)
                {
                    progress?.Report((double)processed / total);
                    continue;
                }

                Console.WriteLine($"[Sync] downloading {entry.Path}");
                var stream = await _client.DownloadAsync(entry.Path, ct);
                if (stream == null)
                {
                    Console.WriteLine($"[Sync] download failed {entry.Path}");
                    failed++;
                    progress?.Report((double)processed / total);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                using (stream)
                using (var fs = File.Create(localPath))
                {
                    await stream.CopyToAsync(fs, ct);
                }
                if (entry.LastModifiedUtc.HasValue)
                    File.SetLastWriteTimeUtc(localPath, entry.LastModifiedUtc.Value);
                Console.WriteLine($"[Sync] downloaded {entry.Path}");
                updated++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] failed {entry.Path}: {ex.Message}");
                failed++;
            }
            finally
            {
                progress?.Report((double)processed / total);
            }
        }

        Console.WriteLine($"Sync {gameName} total={total} updated={updated} skipped={skipped} failed={failed}");
    }
}
