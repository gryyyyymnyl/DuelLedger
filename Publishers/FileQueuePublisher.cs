namespace DuelLedger.Publishers;

using System.Text;
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
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        WriteAtomic(_currentPath, json);
    }

    public void PublishFinal(MatchSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        var name = $"{summary.StartedAt:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}.json";
        var path = Path.Combine(_root, "matches", name);
        WriteAtomic(path, json);

        // 最終状態で current.json も合わせて更新しておく
        PublishSnapshot(new MatchSnapshot(
            summary.Format,
            summary.SelfClass, summary.OppClass, summary.Order,
            summary.StartedAt, summary.EndedAt, summary.Result
        ));
    }

    private static void WriteAtomic(string path, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var tmp = path + ".tmp";
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                }
                if (File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);
                return;
            }
            catch (IOException) when (i < 9)
            {
                Thread.Sleep(50 * (1 << i));
            }
            catch (UnauthorizedAccessException) when (i < 9)
            {
                Thread.Sleep(50 * (1 << i));
            }
        }
    }
}
