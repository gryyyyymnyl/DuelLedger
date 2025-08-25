using System;
using System.Globalization;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Data.Converters;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Converters;

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

public sealed class EqualityConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values.Count >= 2 && Equals(values[0], values[1]);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
