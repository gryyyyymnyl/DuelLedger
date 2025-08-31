using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core.Config;
using DuelLedger.Core.Drives;
using DuelLedger.Core.Templates;

namespace DuelLedger.Infra.Templates;

public sealed class TemplateSyncService
{
    private readonly AppConfig _config;
    private readonly ITemplatePathResolver _resolver;
    private readonly IRemoteDriveClient _client;

    public TemplateSyncService(AppConfig config, ITemplatePathResolver resolver, IRemoteDriveClient client)
    {
        _config = config;
        _resolver = resolver;
        _client = client;
    }

    public async Task SyncAsync(string gameName, CancellationToken ct = default)
    {
        if (_config.Assets.Remote is null)
            return;

        var manifest = await _client.GetManifestAsync(ct);
        var extensions = new HashSet<string>(_config.Assets.Remote.Extensions ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        var root = _resolver.Get(gameName);
        Directory.CreateDirectory(root);

        int total = 0, updated = 0, skipped = 0, failed = 0;

        foreach (var entry in manifest)
        {
            total++;
            var ext = Path.GetExtension(entry.Path);
            if (extensions.Count > 0 && !extensions.Contains(ext))
            {
                Console.WriteLine($"[Sync] skip {entry.Path} (unsupported extension)");
                skipped++;
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
                    continue;

                Console.WriteLine($"[Sync] downloading {entry.Path}");
                var stream = await _client.DownloadAsync(entry.Path, ct);
                if (stream == null)
                {
                    Console.WriteLine($"[Sync] download failed {entry.Path}");
                    failed++;
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
        }

        Console.WriteLine($"Sync {gameName} total={total} updated={updated} skipped={skipped} failed={failed}");
    }
}
