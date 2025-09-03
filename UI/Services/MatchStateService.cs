using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Services;

/// <summary>
/// Tracks whether a match is currently in progress.
/// Implements <see cref="IMatchPublisher"/> so detectors can update state
/// directly without relying on file system notifications.
/// </summary>
public sealed class MatchStateService : IMatchPublisher, INotifyPropertyChanged, IDisposable
{
    private readonly string _currentPath;
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
        try
        {
            if (File.Exists(_currentPath))
            {
                using var fs = new FileStream(
                    _currentPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                var dto = JsonSerializer.Deserialize<MatchSnapshotDto>(fs, _json);
                IsInMatch = dto is not null && dto.StartedAt.HasValue && !dto.EndedAt.HasValue;
            }
        }
        catch
        {
            // ignore and start with default state
        }
    }

    public void PublishSnapshot(MatchSnapshot snapshot)
    {
        IsInMatch = snapshot.StartedAtUtc.HasValue && !snapshot.EndedAtUtc.HasValue;
    }

    public void PublishFinal(MatchSummary summary)
    {
        IsInMatch = false;
    }

    public void Dispose() { }
}

