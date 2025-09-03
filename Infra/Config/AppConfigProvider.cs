using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core.Abstractions;

namespace DuelLedger.Infra.Config;

public sealed class AppConfigProvider : IAppConfig
{
    public AppConfig Value { get; }

    private AppConfigProvider(AppConfig value)
        => Value = value;

    public static async Task<AppConfigProvider> LoadAsync(
        string localPath,
        string? remoteUri = null,
        HttpMessageHandler? handler = null,
        CancellationToken ct = default)
    {
        var local = LoadLocal(localPath);
        AppConfig? remote = null;

        if (!string.IsNullOrWhiteSpace(remoteUri))
        {
            if (remoteUri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                using var http = handler != null ? new HttpClient(handler) : new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(3);
                try
                {
                    var json = await http.GetStringAsync(remoteUri, ct);
                    remote = Deserialize(json);
                }
                catch
                {
                    remote = null;
                }
            }
            else if (File.Exists(remoteUri))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(remoteUri, ct);
                    remote = Deserialize(json);
                }
                catch
                {
                    remote = null;
                }
            }
        }

        var merged = remote is null ? local : Merge(local, remote);
        return new AppConfigProvider(merged);
    }

    private static AppConfig LoadLocal(string path)
    {
        if (!File.Exists(path))
            return new AppConfig();
        var json = File.ReadAllText(path);
        return Deserialize(json);
    }

    private static AppConfig Deserialize(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        return JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
    }

    private static AppConfig Merge(AppConfig local, AppConfig remote)
    {
        var localNode = JsonSerializer.SerializeToNode(local) ?? new JsonObject();
        var remoteNode = JsonSerializer.SerializeToNode(remote);
        if (remoteNode != null)
            MergeNodes(localNode, remoteNode);
        return localNode.Deserialize<AppConfig>() ?? local;
    }

    private static void MergeNodes(JsonNode target, JsonNode source)
    {
        if (target is JsonObject tObj && source is JsonObject sObj)
        {
            foreach (var kv in sObj)
            {
                if (kv.Value is JsonObject sChild)
                {
                    if (tObj[kv.Key] is JsonObject tChild)
                        MergeNodes(tChild, sChild);
                    else
                        tObj[kv.Key] = sChild.DeepClone();
                }
                else
                {
                    tObj[kv.Key] = kv.Value?.DeepClone();
                }
            }
        }
    }
}
