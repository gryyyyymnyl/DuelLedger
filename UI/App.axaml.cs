using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DuelLedger.UI.Views;
using DuelLedger.UI.ViewModels;
using DuelLedger.UI.Services;
using DuelLedger.Publishers;
using DuelLedger.Core;
using DuelLedger.Detectors.Shadowverse;
using DuelLedger.Vision;
using System.IO;
using System.Threading;
using System.Collections.Generic;
namespace DuelLedger.UI;

public partial class App : Application
{
    private DetectionHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var baseDir = AppContext.BaseDirectory;
        Directory.SetCurrentDirectory(baseDir);
        var templateRoot = Path.Combine(baseDir, "Templates");
        var outputRoot = Path.Combine(baseDir, "out");
        Directory.CreateDirectory(Path.Combine(outputRoot, "matches"));
#if NET8_0_WINDOWS
        var nativeDir = Path.Combine(baseDir, "runtimes", "win-x64", "native");
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (!path.Contains(nativeDir, StringComparison.OrdinalIgnoreCase))
            Environment.SetEnvironmentVariable("PATH", nativeDir + ";" + path);
#endif

        Resources["UiMap"] = new UiMapProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel();
            var window = new MainWindow { DataContext = vm };
            desktop.MainWindow = window;

            IGameStateDetectorSet detectorSet;
            try
            {
                detectorSet = new ShadowverseDetectorSet();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to init detectors: {ex.Message}");
                detectorSet = new EmptyDetectorSet();
            }

            IScreenSource screenSource;
#if NET8_0_WINDOWS
            screenSource = new DuelLedger.Vision.Windows.WinScreenSource(detectorSet.ProcessName);
#else
            screenSource = new DummyScreenSource();
#endif

            var publisher = new JsonStreamPublisher(outputRoot);
            _host = new DetectionHost(publisher, detectorSet, screenSource);
            _ = _host.StartAsync(CancellationToken.None);

            Console.WriteLine($"ScreenSource: {screenSource.GetType().Name}");
            Console.WriteLine($"TemplateRoot: {templateRoot}");
            Console.WriteLine($"OutputRoot: {outputRoot}");

            desktop.ShutdownRequested += async (_, e) =>
            {
                e.Cancel = true;
                if (_host != null)
                    await _host.StopAsync();
                desktop.Shutdown();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private sealed class EmptyDetectorSet : IGameStateDetectorSet
    {
        public string GameName => "Shadowverse";
        public string ProcessName => string.Empty;
        public List<IStateDetector> CreateDetectors() => new();
    }
}