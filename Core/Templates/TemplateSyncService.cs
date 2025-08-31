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

            int updated = 0, skipped = 0, failed = 0;
            var sem = new SemaphoreSlim(3);

            var tasks = filtered.Select(async entry =>
            {
                await sem.WaitAsync(ct);
                try
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
                                var meta = localPath + ".sha256";
                                string localHash = File.Exists(meta)
                                    ? File.ReadAllText(meta).Trim()
                                    : ComputeHash(localPath);
                                if (string.Equals(localHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                                {
                                    Interlocked.Increment(ref skipped);
                                    return;
                                }
                            }
                            else if (entry.LastModifiedUtc.HasValue)
                            {
                                var localTime = File.GetLastWriteTimeUtc(localPath);
                                if (localTime >= entry.LastModifiedUtc.Value)
                                {
                                    Interlocked.Increment(ref skipped);
                                    return;
                                }
                            }
                        }

                        await using var stream = await _client.DownloadAsync(entry.Path, ct);
                        await using var fs = File.Create(localPath);
                        await stream.CopyToAsync(fs, ct);
                        if (entry.LastModifiedUtc.HasValue)
                            File.SetLastWriteTimeUtc(localPath, entry.LastModifiedUtc.Value);
                        if (!string.IsNullOrEmpty(entry.Sha256))
                            File.WriteAllText(localPath + ".sha256", entry.Sha256);
                        Interlocked.Increment(ref updated);
                    }
                    catch
                    {
                        Interlocked.Increment(ref failed);
                    }
                }
                finally
                {
                    sem.Release();
                }
            });

            await Task.WhenAll(tasks);

            report.Updated = updated;
            report.Skipped = skipped;
            report.Failed = failed;
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
