using Avalonia;
using System;
using FluentAvalonia.Styling;

namespace DuelLedger.UI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new FluentAvaloniaTheme())
            .LogToTrace();
}