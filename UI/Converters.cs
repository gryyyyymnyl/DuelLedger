using System;
using System.Globalization;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Converters;

public sealed class BoolNotConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public sealed class TurnOrderToText : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is TurnOrder o)
        {
            return map.Get($"TurnOrder.{o}").name;
        }
        return (Application.Current?.Resources["UiMap"] as UiMapProvider)?.Get("TurnOrder.Unknown").name ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ResultToText : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is MatchResult r)
        {
            return map.Get($"Result.{r}").name;
        }
        return (Application.Current?.Resources["UiMap"] as UiMapProvider)?.Get("Result.Unknown").name ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class OpponentTurnOrder : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is TurnOrder o)
        {
            var key = o switch
            {
                TurnOrder.先行 => "TurnOrder.後攻",
                TurnOrder.後攻 => "TurnOrder.先行",
                _ => "TurnOrder.Unknown",
            };
            return map.Get(key).name;
        }
        return (Application.Current?.Resources["UiMap"] as UiMapProvider)?.Get("TurnOrder.Unknown").name ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class OpponentResult : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && value is MatchResult r)
        {
            var key = r switch
            {
                MatchResult.Win => "Result.Lose",
                MatchResult.Lose => "Result.Win",
                MatchResult.Draw => "Result.Draw",
                _ => "Result.Unknown",
            };
            return map.Get(key).name;
        }
        return (Application.Current?.Resources["UiMap"] as UiMapProvider)?.Get("Result.Unknown").name ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// [W/H >= threshold] を判定（しきい値は第3引数の Binding で受け取る）
public sealed class AspectWideByRatio : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3) return false;
        if (values[0] is not double w || values[1] is not double h || h <= 0) return false;
        var threshold = values[2] is double d ? d : 1.6; // 既定 1.6
        return (w / h) >= threshold;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class AspectNarrowByRatio : IMultiValueConverter
{
    private readonly AspectWideByRatio _wide = new();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => !((bool)(_wide.Convert(values, typeof(bool), parameter, culture) ?? false));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class UiTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value as string ?? parameter as string ?? string.Empty;
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map)
        {
            return map.Get(key).name;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class FormatTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map && parameter is string key)
        {
            var fmt = map.Get(key).name;
            return string.Format(fmt, value);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class FormatNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Application.Current?.Resources["UiMap"] is UiMapProvider map)
        {
            var key = value is MatchFormat f
                ? $"Format.{f}"
                : "Format.All";
            return map.Get(key).name;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
public sealed class FormatAbbrevConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            MatchFormat.Rank => "R",
            MatchFormat.TwoPick => "2P",
            MatchFormat.GrandPrix => "GP",
            MatchFormat.Unknown => "?",
            MatchFormat _ => "?",
            _ => "?",
        };
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class TurnOrderBadgeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TurnOrder o ? (o == TurnOrder.先行 ? "先" : o == TurnOrder.後攻 ? "後" : "?") : "?";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class EqualityMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return false;
        return Equals(values[0], values[1]);
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class WinBorderThicknessConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MatchResult r && parameter is string p)
        {
            if ((r == MatchResult.Win && p == "self") || (r == MatchResult.Lose && p == "opp"))
            {
                return new Avalonia.Thickness(3);
            }
        }
        return new Avalonia.Thickness(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class WinBorderBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Application.Current;
        if (app is null)
            return Brushes.Transparent;

        var resources = app.Resources;
        var theme = app.ActualThemeVariant;
        if (value is MatchResult r && parameter is string p)
        {
            if ((r == MatchResult.Win && p == "self") || (r == MatchResult.Lose && p == "opp"))
            {
                var key = p == "self" ? "WinSelfBorderBrush" : "WinOppBorderBrush";
                if (resources.TryGetResource(key, theme, out var brush))
                    return brush;
            }
        }
        return resources.TryGetResource("ThemeBorderBrush", theme, out var defaultBrush)
            ? defaultBrush
            : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class WinBorderBrushMultiConverter : IMultiValueConverter
{
    // values[0]: MatchResult, values[1]: "self"/"opp", values[2]: ThemeVariant
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var app = Application.Current;
        if (app is null) return Brushes.Transparent;

        var resources = app.Resources;
        var theme = values.Count >= 3 && values[2] is ThemeVariant tv ? tv : app.ActualThemeVariant;

        var r = values.Count >= 1 ? values[0] as MatchResult? : null;
        var p = values.Count >= 2 ? values[1] as string : null;
        if (r is MatchResult mr && p is not null)
        {
            if ((mr == MatchResult.Win && p == "self") || (mr == MatchResult.Lose && p == "opp"))
            {
                var key = p == "self" ? "WinSelfBorderBrush" : "WinOppBorderBrush";
                if (resources.TryGetResource(key, theme, out var brush))
                    return brush;
            }
        }
        return resources.TryGetResource("ThemeBorderBrush", theme, out var defaultBrush)
            ? defaultBrush
            : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class NullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => parameter?.ToString() == "invert" ? value != null : value == null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            if (parameter is string p && double.TryParse(p, out var d))
                return b ? d : 0;
            return b ? 1.0 : 0.0;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToAutoHeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? double.NaN : 0.0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
