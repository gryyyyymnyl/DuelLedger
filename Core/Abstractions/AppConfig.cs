using System.Collections.Generic;

namespace DuelLedger.Core.Abstractions;

public sealed class AppConfig
{
    public AssetsConfig Assets { get; set; } = new();
    public Dictionary<string, GameConfig> Games { get; set; } = new();
}

public sealed class AssetsConfig
{
    public string TemplateRoot { get; set; } = "Templates";
    public RemoteConfig? Remote { get; set; }
        = new();
}

public sealed class RemoteConfig
{
    public string Provider { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Manifest { get; set; } = string.Empty;
    public List<string> Extensions { get; set; } = new();
}

public sealed class GameConfig
{
    public string TemplatesSubdir { get; set; } = string.Empty;
    public Dictionary<string, string> Keys { get; set; } = new();
}

