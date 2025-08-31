using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core.Config;

namespace DuelLedger.Core.Templates;

public sealed class TemplateSyncService : ITemplateSyncService
{
    private readonly AppConfig _config;
    private readonly IRemoteDriveClient _client;

    public TemplateSyncService(AppConfig config, IRemoteDriveClient client)
    {
        _config = config;
        _client = client;
    }

    public async Task<TemplateSyncReport> SyncAsync(string gameName, CancellationToken ct)
    {
        var report = new TemplateSyncReport();
        try
        {
            var entries = await _client.GetManifestAsync(ct);
            var sub = _config.Games.TryGetValue(gameName, out var g) && !string.IsNullOrWhiteSpace(g.TemplatesSubdir)
                ? g.TemplatesSubdir : gameName.ToLowerInvariant();
            var baseDir = Path.Combine(AppContext.BaseDirectory, _config.Assets.TemplateRoot);
            var filtered = entries.Where(e => e.Path.StartsWith(sub + "/", StringComparison.OrdinalIgnoreCase)).ToList();
            report.Total = filtered.Count;

            foreach (var entry in filtered)
            {
                ct.ThrowIfCancellationRequested();
                var localPath = Path.Combine(baseDir, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                try
                {
                    if (File.Exists(localPath))
                    {
                        if (!string.IsNullOrEmpty(entry.Sha256))
                        {
                            var localHash = ComputeHash(localPath);
                            if (string.Equals(localHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                            {
                                report.Skipped++;
                                continue;
                            }
                        }
                        else if (entry.LastModifiedUtc.HasValue)
                        {
                            var localTime = File.GetLastWriteTimeUtc(localPath);
                            if (localTime >= entry.LastModifiedUtc.Value)
                            {
                                report.Skipped++;
                                continue;
                            }
                        }
                    }

                    using var stream = await _client.DownloadAsync(entry.Path, ct);
                    using var fs = File.Create(localPath);
                    await stream.CopyToAsync(fs, ct);
                    if (entry.LastModifiedUtc.HasValue)
                        File.SetLastWriteTimeUtc(localPath, entry.LastModifiedUtc.Value);
                    report.Updated++;
                }
                catch
                {
                    report.Failed++;
                }
            }
        }
        catch
        {
            report.Failed = report.Total == 0 ? 1 : report.Failed + 1;
        }

        return report;
    }

    private static string ComputeHash(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }
}
