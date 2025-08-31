using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Converters;

public sealed class ClassIconConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();
    private static readonly SvgIconCache _cache = SvgIconCache.Instance;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is not UiMapProvider map || value is not PlayerClass cls)
            return null;

        var size = parameter is double d ? new Size(d, d) : new Size(24, 24);
        var theme = (Application.Current as Application)?.ActualThemeVariant ?? ThemeVariant.Light;
        var key = BuildKey(cls, size, theme);
        var bmp = _cache.TryGet(key);
        if (bmp != null)
            return bmp;

        if (_tokens.TryGetValue(key, out var existingCts))
            existingCts.Cancel();
        var cts = new CancellationTokenSource();
        _tokens[key] = cts;
        var uri = new Uri(map.Get($"Class.{cls}").icon);
        _ = _cache.GetOrCreateAsync(key, uri, size, cts.Token)
            .ContinueWith(t => _tokens.TryRemove(key, out _), TaskScheduler.Default);
        return _cache.Placeholder;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static string BuildKey(PlayerClass cls, Size size, ThemeVariant theme)
        => $"{(int)cls}_{(int)size.Width}x{(int)size.Height}_{theme}";

    public static void Cancel(string key)
    {
        if (_tokens.TryRemove(key, out var cts))
            cts.Cancel();
    }
}

public sealed class ClassIconKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlayerClass cls)
            return null;
        var size = parameter is double d ? new Size(d, d) : new Size(24, 24);
        var theme = (Application.Current as Application)?.ActualThemeVariant ?? ThemeVariant.Light;
        return ClassIconConverter.BuildKey(cls, size, theme);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
