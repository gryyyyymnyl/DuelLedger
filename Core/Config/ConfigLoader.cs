using System.IO;
using System.Text.Json;

namespace DuelLedger.Core.Config;

public static class ConfigLoader
{
    public static AppConfig Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AppConfig();
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, options);
            return cfg ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }
}
