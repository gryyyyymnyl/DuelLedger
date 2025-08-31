using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Services;

public sealed class SvgIconStore
{
    public static SvgIconStore Instance { get; } = new();
    private readonly SvgIconCache _cache = new();
    private readonly ConcurrentDictionary<string, string> _urlByKey = new();

    private SvgIconStore() { }

    public event Action<string, string>? IconReady
    {
        add { _cache.IconReady += value; }
        remove { _cache.IconReady -= value; }
    }

    public string? TryGetLocal(string key) => _cache.TryGetLocalOnly(key);

    public void Register(string key, string iconUrl)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(iconUrl))
            _urlByKey[key] = iconUrl;
    }

    public async Task WarmAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var tasks = new List<Task>();
        foreach (var k in keys.Distinct())
        {
            if (_urlByKey.TryGetValue(k, out var url))
                tasks.Add(_cache.FetchAsync(k, url, ct));
        }
        await Task.WhenAll(tasks);
    }
}
