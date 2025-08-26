using System.Collections.Generic;
using System;
using Avalonia.Controls;

namespace DuelLedger.UI.Services;

public sealed class TransparencyService
{
    public bool IsAcrylicSupported()
        => OperatingSystem.IsWindows();

    public IEnumerable<WindowTransparencyLevel> BuildHint(string mode)
    {
        return mode switch
        {
            "AcrylicBlur" => IsAcrylicSupported()
                ? new[] { WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur }
                : new[] { WindowTransparencyLevel.None },
            "Blur" => new[] { WindowTransparencyLevel.Blur },
            _ => new[] { WindowTransparencyLevel.None },
        };
    }
}
