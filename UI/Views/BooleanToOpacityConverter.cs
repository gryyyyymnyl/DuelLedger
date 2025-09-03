using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DuelLedger.UI.Views;

public sealed class BooleanToOpacityConverter : IValueConverter
{
    public double TrueOpacity { get; set; } = 1;
    public double FalseOpacity { get; set; } = 0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueOpacity : FalseOpacity;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
