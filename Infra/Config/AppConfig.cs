namespace DuelLedger.Infra.Config;

public sealed class AppConfig
{
    public RemoteConfig? Remote { get; set; }
}

public sealed class RemoteConfig
{
    public string? StaticBaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 20;
    public string Manifest { get; set; } = "manifest.json";
}
