using System.IO;
using System.Text.Json;

namespace DuelLedger.Core.Config;

public static class ConfigLoader
{
    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            return new AppConfig();

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        return JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
    }
}

