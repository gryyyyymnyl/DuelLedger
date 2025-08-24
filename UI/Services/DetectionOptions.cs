using System.Text.Json;
using System.Text.Json.Nodes;

namespace DuelLedger.UI.Services;

public sealed record DetectionOptions(string OutputDirectory, string TemplateDirectory, string ProcessName)
{
    public static DetectionOptions Load(string? baseDir = null)
    {
        var rootDir = baseDir ?? AppContext.BaseDirectory;
        try
        {
            var path = Path.Combine(rootDir, "appsettings.json");
            if (File.Exists(path))
            {
                using var fs = File.OpenRead(path);
                var doc = JsonNode.Parse(fs);
                var det = doc?["Detection"] as JsonObject;
                return new DetectionOptions(
                    det? ["OutputDirectory"]?.GetValue<string>() ?? "out",
                    det? ["TemplateDirectory"]?.GetValue<string>() ?? "Templates",
                    det? ["ProcessName"]?.GetValue<string>() ?? "Shadowverse"
                );
            }
        }
        catch { }
        return new DetectionOptions("out", "Templates", "Shadowverse");
    }
}
