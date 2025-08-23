using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DuelLedger.UI.Models;

using DuelLedger.Contracts;
namespace DuelLedger.UI.Converters;

public sealed class PlayerClassToBrush : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PlayerClass c)
        {
            var color = c switch
            {
                PlayerClass.エルフ => Color.Parse("#6AB04A"),
                PlayerClass.ロイヤル => Color.Parse("#487EB0"),
                PlayerClass.ウィッチ => Color.Parse("#9B59B6"),
                PlayerClass.ドラゴン => Color.Parse("#E67E22"),
                PlayerClass.ナイトメア => Color.Parse("#C0392B"),
                PlayerClass.ビショップ => Color.Parse("#F1C40F"),
                PlayerClass.ネメシス => Color.Parse("#7F8C8D"),
                _ => Color.Parse("#95A5A6"),
            };
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Color.Parse("#95A5A6"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class PlayerClassToGlyph : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is PlayerClass c ? c switch
        {
            PlayerClass.エルフ => "エ",
            PlayerClass.ロイヤル => "ロ",
            PlayerClass.ウィッチ => "ウ",
            PlayerClass.ドラゴン => "ド",
            PlayerClass.ナイトメア => "ナ",
            PlayerClass.ビショップ => "ビ",
            PlayerClass.ネメシス => "ネ",
            _ => "?",
        } : "?";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class TurnOrderToText : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TurnOrder o ? o switch
        {
            TurnOrder.先行 => "先",
            TurnOrder.後攻 => "後",
            _ => "-",
        } : "-";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ResultToText : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MatchResult r ? r switch
        {
            MatchResult.Win => "勝",
            MatchResult.Lose => "負",
            MatchResult.Draw => "ー",
            _ => "-",
        } : "-";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class OpponentTurnOrder : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TurnOrder o ? o switch
        {
            TurnOrder.先行 => "後",
            TurnOrder.後攻 => "先",
            _ => "-",
        } : "-";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class OpponentResult : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MatchResult r ? r switch
        {
            MatchResult.Win => "負",
            MatchResult.Lose => "勝",
            MatchResult.Draw => "ー",
            _ => "-",
        } : "-";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
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
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class AspectNarrowByRatio : IMultiValueConverter
{
    private readonly AspectWideByRatio _wide = new();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => !((bool)(_wide.Convert(values, typeof(bool), parameter, culture) ?? false));
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}