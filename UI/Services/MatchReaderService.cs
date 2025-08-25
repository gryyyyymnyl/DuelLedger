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
    private readonly FileSystemWatcher _watcher;
    private readonly JsonSerializerOptions _json = new(){ PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, MatchRecord> _byFile = new();
    private readonly List<(string Path, MatchRecord Rec)> _pending = new();
    private readonly DispatcherTimer _debounce;

    public ObservableCollection<MatchRecord> Items { get; } = new();

    public MatchReaderService(string baseDir)
    {
        _baseDir = baseDir;
        _matchesDir = Path.Combine(_baseDir, "matches");
        Directory.CreateDirectory(_matchesDir);

        _watcher = new FileSystemWatcher(_matchesDir, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
        };
        _watcher.Created += (_, e) => _ = TryLoadAsync(e.FullPath);
        _watcher.Changed += (_, e) => _ = TryLoadAsync(e.FullPath);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _debounce.Tick += (_, __) => Flush();
    }

    public void LoadInitial()
    {
        foreach (var path in Directory.EnumerateFiles(_matchesDir, "*.json").OrderBy(p => p))
        {
            _ = TryLoadAsync(path);
        }
    }

    private async Task TryLoadAsync(string path)
    {
        // 書き込み中を考慮してリトライ
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var dto = await JsonSerializer.DeserializeAsync<MatchSummaryDto>(fs, _json);
                if (dto is null) return;
                var rec = dto.ToDomain();
                await Dispatcher.UIThread.InvokeAsync(() => Enqueue(path, rec));
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

    private void Enqueue(string path, MatchRecord rec)
    {
        _pending.RemoveAll(p => p.Path == path);
        _pending.Add((path, rec));
        _debounce.Stop();
        _debounce.Start();
    }

    private void Flush()
    {
        _debounce.Stop();
        var list = _pending
            .OrderByDescending(x => x.Rec.EndedAt)
            .ThenByDescending(x => x.Rec.StartedAt)
            .ThenByDescending(x => x.Rec.Result)
            .ThenBy(x => x.Rec.SelfClass)
            .ThenBy(x => x.Rec.OppClass)
            .ThenBy(x => x.Rec.Order)
            .ToList();
        _pending.Clear();

        foreach (var (path, rec) in list)
            UpsertCore(path, rec);
    }

    private void UpsertCore(string path, MatchRecord rec)
    {
        if (_byFile.TryGetValue(path, out var old))
        {
            var idx = Items.IndexOf(old);
            if (idx >= 0) Items.RemoveAt(idx);
            _byFile[path] = rec;
        }
        else
        {
            _byFile[path] = rec;
        }

        InsertAtSortPosition(rec);
    }

    private void InsertAtSortPosition(MatchRecord rec)
    {
        int idx = 0;
        while (idx < Items.Count && CompareNewFirst(rec, Items[idx]) >= 0) idx++;
        Items.Insert(idx, rec);
    }

    private static int CompareNewFirst(MatchRecord a, MatchRecord b)
    {
        int c;
        if ((c = b.EndedAt.CompareTo(a.EndedAt)) != 0) return c;
        if ((c = b.StartedAt.CompareTo(a.StartedAt)) != 0) return c;
        if ((c = b.Result.CompareTo(a.Result)) != 0) return c;
        if ((c = a.SelfClass.CompareTo(b.SelfClass)) != 0) return c;
        if ((c = a.OppClass.CompareTo(b.OppClass)) != 0) return c;
        if ((c = a.Order.CompareTo(b.Order)) != 0) return c;
        return 0;
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}