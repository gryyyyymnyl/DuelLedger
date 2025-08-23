namespace DuelLedger.Publishers;

using System.Text.Json;

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
        var tmp = _currentPath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _currentPath, overwrite: true); // アトミック更新
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

public sealed class NullPublisher : IMatchPublisher
{
    public void PublishSnapshot(MatchSnapshot s) { }
    public void PublishFinal(MatchSummary s) { }
}