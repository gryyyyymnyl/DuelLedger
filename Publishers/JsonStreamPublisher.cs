namespace DuelLedger.Publishers;

using System.Text.Json;
using System.Threading;

using DuelLedger.Contracts;
using DuelLedger.Core;
public sealed class JsonStreamPublisher : IMatchPublisher
{
    private readonly string _root;
    private readonly string _currentPath;

    public JsonStreamPublisher(string baseDir)
    {
        _root = baseDir;
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "matches"));
        _currentPath = Path.Combine(_root, "current.json");
    }

    public void PublishSnapshot(MatchSnapshot snapshot)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(snapshot, new JsonSerializerOptions { WriteIndented = true });
        var tmp = Path.Combine(_root, $"current.{Environment.ProcessId}.tmp");
        using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            fs.Write(json, 0, json.Length);
            fs.Flush(true);
        }
        ReplaceWithRetry(tmp, _currentPath, maxRetry: 12, initialDelayMs: 30);
    }

    private static void ReplaceWithRetry(string tmp, string dst, int maxRetry, int initialDelayMs)
    {
        var delay = initialDelayMs;
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                if (!File.Exists(dst))
                {
                    File.Move(tmp, dst);
                }
                else
                {
                    File.Replace(tmp, dst, destinationBackupFileName: null);
                }
                return;
            }
            catch (UnauthorizedAccessException) when (i < maxRetry - 1)
            {
                Thread.Sleep(delay);
                delay = Math.Min(delay * 2, 500);
            }
            catch (IOException) when (i < maxRetry - 1)
            {
                Thread.Sleep(delay);
                delay = Math.Min(delay * 2, 500);
            }
        }
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        throw new IOException($"Atomic replace failed for '{Path.GetFullPath(dst)}'. Another process is denying delete/replace.");
    }

    public void PublishFinal(MatchSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        var name = $"{summary.StartedAt:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}.json";
        var path = Path.Combine(_root, "matches", name);
        File.WriteAllText(path, json);

        // 最終状態で current.json も合わせて更新しておく
        PublishSnapshot(new MatchSnapshot(
            summary.Format,
            summary.SelfClass, summary.OppClass, summary.Order,
            summary.StartedAt, summary.EndedAt, summary.Result
        ));
    }
}
