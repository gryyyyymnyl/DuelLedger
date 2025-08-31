using System;
using System.IO;
using DuelLedger.Core.Templates;

namespace DuelLedger.Infra.Templates;

public class TemplatePathResolver : ITemplatePathResolver
{
    public string Get(string gameName)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, "Templates", gameName.ToLowerInvariant());
        return Directory.Exists(candidate) ? candidate : Path.Combine(baseDir, "Templates");
    }
}
