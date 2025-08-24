using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Converters;

public sealed class ClassColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resources = Application.Current?.Resources;
        if (resources != null &&
            resources.TryGetValue("ClassVisuals", out var obj) &&
            obj is IReadOnlyDictionary<PlayerClass, ClassVisual> visuals &&
            value is PlayerClass cls &&
            visuals.TryGetValue(cls, out var visual))
        {
            return new SolidColorBrush(Color.Parse(visual.Color));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
