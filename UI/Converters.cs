using System;
using System.Globalization;
using System.Collections.Generic;
using Avalonia.Data.Converters;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Converters;

public sealed class TurnOrderToText : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TurnOrder o ? o switch
        {
            TurnOrder.先行 => Res.Get("TurnOrder_First"),
            TurnOrder.後攻 => Res.Get("TurnOrder_Second"),
            _ => Res.Get("TurnOrder_Unknown"),
        } : Res.Get("TurnOrder_Unknown");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ResultToText : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MatchResult r ? r switch
        {
            MatchResult.Win => Res.Get("Result_Win"),
            MatchResult.Lose => Res.Get("Result_Lose"),
            MatchResult.Draw => Res.Get("Result_Draw"),
            _ => Res.Get("Result_Unknown"),
        } : Res.Get("Result_Unknown");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class OpponentTurnOrder : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TurnOrder o ? o switch
        {
            TurnOrder.先行 => Res.Get("TurnOrder_Second"),
            TurnOrder.後攻 => Res.Get("TurnOrder_First"),
            _ => Res.Get("TurnOrder_Unknown"),
        } : Res.Get("TurnOrder_Unknown");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class OpponentResult : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MatchResult r ? r switch
        {
            MatchResult.Win => Res.Get("Result_Lose"),
            MatchResult.Lose => Res.Get("Result_Win"),
            MatchResult.Draw => Res.Get("Result_Draw"),
            _ => Res.Get("Result_Unknown"),
        } : Res.Get("Result_Unknown");

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
