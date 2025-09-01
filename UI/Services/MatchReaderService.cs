using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Services;

public sealed class MatchReaderService : IDisposable
{
    private readonly string _baseDir;
    private readonly string _matchesDir;
    private readonly string _currentPath;
    private readonly FileSystemWatcher _watcher;
    private readonly FileSystemWatcher _currentWatcher;
    private readonly JsonSerializerOptions _json = new(){ PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, MatchRecord> _byFile = new();

    public ObservableCollection<MatchRecord> Items { get; } = new();

    public MatchReaderService(string baseDir)
    {
        _baseDir = baseDir;
        _matchesDir = Path.Combine(_baseDir, "matches");
        Directory.CreateDirectory(_matchesDir);
        _currentPath = Path.Combine(_baseDir, "current.json");

        _watcher = new FileSystemWatcher(_matchesDir, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
        };
        _watcher.Created += (_, e) => _ = TryLoadAsync(e.FullPath);
        _watcher.Changed += (_, e) => _ = TryLoadAsync(e.FullPath);

        _currentWatcher = new FileSystemWatcher(_baseDir, "current.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
        };
        _currentWatcher.Created += (_, e) => _ = TryLoadCurrentAsync(e.FullPath);
        _currentWatcher.Changed +=  (_, e) => _ = TryLoadCurrentAsync(e.FullPath);
        _currentWatcher.Renamed += (_, e) => _ = TryLoadCurrentAsync(e.FullPath);
    }

    public void LoadInitial()
    {
        foreach (var path in Directory.EnumerateFiles(_matchesDir, "*.json").OrderBy(p => p))
        {
            _ = TryLoadAsync(path);
        }
        if (File.Exists(_currentPath))
        {
            _ = TryLoadCurrentAsync(_currentPath);
        }
    }

    private async Task TryLoadAsync(string path)
    {
        // 書き込み中を考慮してリトライ
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var dto = await JsonSerializer.DeserializeAsync<MatchSummaryDto>(fs, _json);
                if (dto is null) return;
                var rec = dto.ToDomain();
                await Dispatcher.UIThread.InvokeAsync(() => Upsert(path, rec));
                return;
            }
            catch (IOException)
            {
                await Task.Delay(60);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(60);
            }
            catch
            {
                return; // 形式不正などは捨てる
            }
        }
    }

    private async Task TryLoadCurrentAsync(string path)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var dto = await JsonSerializer.DeserializeAsync<MatchSnapshotDto>(fs, _json);
                if (dto is null) return;
                var rec = dto.ToDomain();
                await Dispatcher.UIThread.InvokeAsync(() => UpsertCurrent(path, rec));
                return;
            }
            catch (IOException)
            {
                await Task.Delay(60);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(60);
            }
            catch
            {
                return;
            }
        }
    }

    private void Upsert(string path, MatchRecord rec)
    {
        if (_byFile.TryGetValue(path, out var old))
        {
            // 置き換え（通常は不要だが Changed で来た場合）
            var idx = Items.IndexOf(old);
            if (idx >= 0) Items[idx] = rec;
            _byFile[path] = rec;
            Resort();
        }
        else
        {
            _byFile[path] = rec;
            Items.Add(rec);
            Resort();
        }
    }

    private void UpsertCurrent(string path, MatchRecord rec)
    {
        if (rec.IsInProgress
            && rec.Format != MatchFormat.Unknown
            && rec.SelfClass != PlayerClass.Unknown
            && rec.OppClass != PlayerClass.Unknown
            && rec.Order != TurnOrder.Unknown)
        {
            Upsert(path, rec);
        }
        else
        {
            if (_byFile.TryGetValue(path, out var old))
            {
                Items.Remove(old);
                _byFile.Remove(path);
                Resort();
            }
        }
    }

    private void Resort()
    {
        // UTC基準 + 複合キーで安定ソート（新しい順）：同一終了時刻でも順番が揺れないように
        var ordered = Items
            .OrderByDescending(x => x.EndedAt)
            .ThenByDescending(x => x.StartedAt)
            .ThenByDescending(x => x.Result)
            .ThenBy(x => x.SelfClass)
            .ThenBy(x => x.OppClass)
            .ThenBy(x => x.Order)
            .ToList();
        Items.Clear();
        foreach (var x in ordered) Items.Add(x);
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _currentWatcher.Dispose();
    }
}