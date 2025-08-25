using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Converters;

public sealed class ClassIconConverter : IValueConverter
{
    private readonly SvgIconCache _cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is PlayerClass cls)
        {
            var item = map.Get($"Class.{cls}");
            var path = _cache.GetLocalPath(cls.ToString(), item.iconUrl);
            if (!string.IsNullOrEmpty(path)) return path;
            if (!string.IsNullOrEmpty(item.icon) && item.icon.StartsWith("avares://"))
                return item.icon;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
