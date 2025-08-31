using System.Collections.Generic;

namespace DuelLedger.Core.Config;

public sealed class AppConfig
{
    public AssetsConfig Assets { get; set; } = new();
    public Dictionary<string, GameConfig> Games { get; set; } = new();
}

public sealed class AssetsConfig
{
    public string TemplateRoot { get; set; } = "Templates";
}

public sealed class GameConfig
{
    public string TemplatesSubdir { get; set; } = "";
    public Dictionary<string, string> Keys { get; set; } = new();
}
