using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core.Util;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Services;

/// <summary>
/// Watches <c>current.json</c> and exposes whether a match is ongoing.
/// </summary>
public sealed class MatchStateService : INotifyPropertyChanged, IDisposable
{
    private readonly string _currentPath;
    private readonly FileSystemWatcher _watcher;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private readonly IFileSystem _fs;
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

    public MatchStateService(string baseDir, IFileSystem? fileSystem = null)
    {
        _fs = fileSystem ?? SystemFileSystem.Instance;
        _currentPath = Path.Combine(baseDir, "current.json");
        _watcher = new FileSystemWatcher(baseDir, "current.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false,
        };
        _watcher.Created += OnChanged;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnChanged;
        _ = TryUpdateAsync();
    }

    private void OnChanged(object? sender, FileSystemEventArgs e) => _ = TryUpdateAsync();

    private async Task TryUpdateAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await using var fs = _fs.OpenReadShared(_currentPath);
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
                return; // ignore malformed
            }
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
