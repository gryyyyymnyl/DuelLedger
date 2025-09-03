namespace DuelLedger.Publishers;

using System;
using System.IO;
using System.Text.Json;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core;
using DuelLedger.Core.Util;
public sealed class JsonStreamPublisher : IMatchPublisher
{
    private readonly string _root;
    private readonly string _currentPath;
    private readonly IFileSystem _fs;

    public JsonStreamPublisher(string baseDir, IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? SystemFileSystem.Instance;
        _root = baseDir;
        _fs.EnsureDirectory(_root);
        _fs.EnsureDirectory(Path.Combine(_root, "matches"));
        _currentPath = Path.Combine(_root, "current.json");
    }

    public void PublishSnapshot(MatchSnapshot snapshot)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(snapshot, new JsonSerializerOptions { WriteIndented = true });
        Retry.Run(() => _fs.WriteAtomic(_currentPath, json));
    }

    public void PublishFinal(MatchSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        var name = $"{summary.StartAt:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}.json";
        var path = Path.Combine(_root, "matches", name);
        Retry.Run(() => _fs.WriteAllText(path, json));

        // 最終状態で current.json も合わせて更新しておく
        PublishSnapshot(new MatchSnapshot(
            summary.Format,
            summary.SelfClass, summary.OppClass, summary.Order,
            summary.StartAt, summary.EndAt, summary.Result
        ));
    }
}
