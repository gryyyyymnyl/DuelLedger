using Avalonia;
using Avalonia.Controls;
using System;

namespace DuelLedger.UI.Platform.Windows;

public static class Win32WindowEx
{
#if WINDOWS
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
#endif

    public static void EnableClickThrough(Window window, bool enable)
    {
#if WINDOWS
        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero) return;
        var styles = GetWindowLongPtr(handle, GWL_EXSTYLE).ToInt64();
        if (enable)
            styles |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        else
            styles &= ~(WS_EX_TRANSPARENT | WS_EX_LAYERED);
        SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(styles));
#endif
    }
}
