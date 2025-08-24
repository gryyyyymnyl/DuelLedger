using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Converters;

public sealed class ClassColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is PlayerClass cls)
        {
            var color = map.Get($"Class.{cls}").color;
            if (string.IsNullOrEmpty(color)) color = "#808080";
            return new SolidColorBrush(Color.Parse(color));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
