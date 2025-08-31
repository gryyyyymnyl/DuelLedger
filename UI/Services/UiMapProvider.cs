using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Services;

public sealed class UiMapProvider
{
    private readonly IReadOnlyDictionary<string, UiMapItem> _map;

    public UiMapProvider()
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _map = Load(lang) ?? Load(null) ?? new Dictionary<string, UiMapItem>();
    }

    private static IReadOnlyDictionary<string, UiMapItem>? Load(string? lang)
    {
        var name = lang == null ? "ui_map.json" : $"ui_map.{lang}.json";
        var uri = new Uri($"avares://DuelLedger.UI/{name}");
        if (!AssetLoader.Exists(uri)) return null;
        using var s = AssetLoader.Open(uri);
        return JsonSerializer.Deserialize(s, UiJsonContext.Default.DictionaryStringUiMapItem);
    }

    public UiMapItem Get(string key)
        => _map.TryGetValue(key, out var item) ? item : UiMapItem.Default;
}

public sealed class UiMapItem
{
    public string name { get; set; } = string.Empty;
    public string icon { get; set; } = string.Empty;
    public string iconUrl { get; set; } = string.Empty;
    public string color { get; set; } = "#808080";
    public static UiMapItem Default { get; } = new();
}
