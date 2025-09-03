using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DuelLedger.UI.Views;

public sealed class BooleanToOffsetYConverter : IValueConverter
{
    public double VisibleOffset { get; set; } = 0;
    public double HiddenOffset { get; set; } = -6;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? VisibleOffset : HiddenOffset;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
