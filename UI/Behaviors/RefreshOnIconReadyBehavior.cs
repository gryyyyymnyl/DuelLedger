using System;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using DuelLedger.UI.Services;
using DuelLedger.UI.Converters;

namespace DuelLedger.UI.Behaviors;

public sealed class RefreshOnIconReadyBehavior : Behavior<Image>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        SvgIconCache.Instance.IconReady += OnIconReady;
    }

    protected override void OnDetaching()
    {
        SvgIconCache.Instance.IconReady -= OnIconReady;
        if (AssociatedObject?.Tag is string key)
            ClassIconConverter.Cancel(key);
        base.OnDetaching();
    }

    private void OnIconReady(object? sender, string key)
    {
        var img = AssociatedObject;
        if (img?.Tag is string tag && tag == key)
        {
            var bmp = SvgIconCache.Instance.TryGet(key);
            if (bmp != null)
                img.Source = bmp;
        }
    }
}
