using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Converters;

public sealed class ClassIconConverter : IValueConverter
{
    private readonly SvgIconStore _store = SvgIconStore.Instance;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is PlayerClass cls)
        {
            var item = map.Get($"Class.{cls}");
            SvgIconStore.Instance.Register(cls.ToString(), item.iconUrl);
            var path = _store.TryGetLocal(cls.ToString());
            if (targetType == typeof(bool))
            {
                if (!string.IsNullOrEmpty(path)) return false;
                if (!string.IsNullOrEmpty(item.icon) && item.icon.StartsWith("avares://", StringComparison.Ordinal)) return false;
                return true;
            }
            if (!string.IsNullOrEmpty(path)) return path;
            if (!string.IsNullOrEmpty(item.icon) && item.icon.StartsWith("avares://", StringComparison.Ordinal))
                return item.icon;
        }
        return targetType == typeof(bool) ? true : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
