using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Converters;

public sealed class ClassNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is PlayerClass cls)
        {
            return map.Get($"Class.{cls}").name;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
