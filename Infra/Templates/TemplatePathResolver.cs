using System;
using System.IO;
using DuelLedger.Core.Abstractions;
using DuelLedger.Core.Templates;

namespace DuelLedger.Infra.Templates;

public class TemplatePathResolver : ITemplatePathResolver
{
    private readonly IAppConfig _config;

    public TemplatePathResolver(IAppConfig config)
    {
        _config = config;
    }

    public string Get(string gameName)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, _config.Value.Assets.TemplateRoot);
        var sub = _config.Value.Games.TryGetValue(gameName, out var g) &&
                  !string.IsNullOrWhiteSpace(g.TemplatesSubdir)
            ? g.TemplatesSubdir
            : gameName.ToLowerInvariant();
        var candidate = Path.Combine(baseDir, sub);
        return Directory.Exists(candidate) ? candidate : baseDir;
    }
}
