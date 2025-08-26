using System;
using System.IO;
using System.Text.Json;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Services;

public interface ISettingsService
{
    UiSettings Current { get; }
    void Save();
}

public sealed class SettingsService : ISettingsService
{
    private readonly string _path;
    public UiSettings Current { get; }

    public SettingsService()
    {
        _path = Path.Combine(AppContext.BaseDirectory, "ui-settings.json");
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                Current = JsonSerializer.Deserialize<UiSettings>(json) ?? new UiSettings();
            }
            else
            {
                Current = new UiSettings();
            }
        }
        catch
        {
            Current = new UiSettings();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch
        {
            // ignore
        }
    }
}
