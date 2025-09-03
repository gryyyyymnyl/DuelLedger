namespace DuelLedger.Publishers;

using System.Text.Json;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core;
using DuelLedger.Core.Util;
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
        try
        {
            Retry.Run(() =>
            {
                if (!File.Exists(_currentPath))
                {
                    File.Move(tmp, _currentPath);
                }
                else
                {
                    File.Replace(tmp, _currentPath, destinationBackupFileName: null);
                }
            });
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    public void PublishFinal(MatchSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        var name = $"{summary.StartedAt:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}.json";
        var path = Path.Combine(_root, "matches", name);
        Retry.Run(() => File.WriteAllText(path, json));

        // 最終状態で current.json も合わせて更新しておく
        PublishSnapshot(new MatchSnapshot(
            summary.Format,
            summary.SelfClass, summary.OppClass, summary.Order,
            summary.StartedAt, summary.EndedAt, summary.Result
        ));
    }
}
