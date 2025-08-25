namespace DuelLedger.Net;

public class CachedSvgProvider : ISvgProvider
{
    private readonly ISvgProvider _inner;
    private readonly string _cacheDir;

    public CachedSvgProvider(ISvgProvider inner, string? cacheDir = null)
    {
        _inner = inner;
        _cacheDir = cacheDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SWBT", "svgcache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<string?> GetSvgAsync(int classId)
    {
        var path = Path.Combine(_cacheDir, classId + ".svg");
        if (File.Exists(path))
            return await File.ReadAllTextAsync(path);
        var svg = await _inner.GetSvgAsync(classId);
        if (svg != null)
            await File.WriteAllTextAsync(path, svg);
        return svg;
    }
}
