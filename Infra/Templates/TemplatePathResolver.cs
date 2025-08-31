using System;
using System.IO;
using DuelLedger.Core.Config;
using DuelLedger.Core.Templates;

namespace DuelLedger.Infra.Templates;

public class TemplatePathResolver : ITemplatePathResolver
{
    private readonly AppConfig _config;

    public TemplatePathResolver(AppConfig config)
    {
        _config = config;
    }

    public string Get(string gameName)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, _config.Assets.TemplateRoot);
        var sub = _config.Games.TryGetValue(gameName, out var g) &&
                  !string.IsNullOrWhiteSpace(g.TemplatesSubdir)
            ? g.TemplatesSubdir
            : gameName.ToLowerInvariant();
        var candidate = Path.Combine(baseDir, sub);
        return Directory.Exists(candidate) ? candidate : baseDir;
    }
}
