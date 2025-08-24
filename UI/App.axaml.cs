using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DuelLedger.UI.Models;
using DuelLedger.UI.Views;
using DuelLedger.UI.ViewModels;
using DuelLedger.UI.Converters;
namespace DuelLedger.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Resources["ClassVisuals"] = LoadClassVisuals();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel();
            var window = new MainWindow { DataContext = vm };
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IReadOnlyDictionary<PlayerClass, ClassVisual> LoadClassVisuals()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "ClassAssets.json");
        if (!File.Exists(path)) return new Dictionary<PlayerClass, ClassVisual>();

        using var stream = File.OpenRead(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, ClassVisual>>(stream) ?? new();
        var dict = new Dictionary<PlayerClass, ClassVisual>();
        foreach (var (key, value) in raw)
        {
            if (System.Enum.TryParse<PlayerClass>(key, out var cls))
            {
                dict[cls] = value;
            }
        }
        return dict;
    }
}