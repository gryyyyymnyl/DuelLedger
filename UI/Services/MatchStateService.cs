using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.ComponentModel;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Services;

public sealed class MatchStateService : INotifyPropertyChanged, IDisposable
{
    private readonly string _currentPath;
    private readonly FileSystemWatcher _watcher;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private bool _isInMatch;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsInMatch
    {
        get => _isInMatch;
        private set
        {
            if (_isInMatch != value)
            {
                _isInMatch = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInMatch)));
            }
        }
    }

    public MatchStateService(string baseDir)
    {
        _currentPath = Path.Combine(baseDir, "current.json");
        _watcher = new FileSystemWatcher(baseDir, "current.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
        };
        _watcher.Created += (_, e) => _ = TryLoadAsync(e.FullPath);
        _watcher.Changed += (_, e) => _ = TryLoadAsync(e.FullPath);
        _watcher.Renamed += (_, e) => _ = TryLoadAsync(e.FullPath);

        if (File.Exists(_currentPath))
            _ = TryLoadAsync(_currentPath);
    }

    private async Task TryLoadAsync(string path)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var dto = await JsonSerializer.DeserializeAsync<MatchSnapshotDto>(fs, _json);
                IsInMatch = dto is not null && dto.StartedAt.HasValue && !dto.EndedAt.HasValue;
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

    public void Dispose()
    {
        _watcher.Dispose();
    }
}

