using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DuelLedger.UI.Views;
using DuelLedger.UI.ViewModels;
using DuelLedger.UI.Services;
using DuelLedger.Detectors.Shadowverse;
using DuelLedger.Publishers;
using DuelLedger.Vision;
using DuelLedger.Core;
#if WINDOWS
using DuelLedger.Vision.Windows;
#endif
using System.Threading;
using System.IO;
namespace DuelLedger.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private DetectionHost? _detectionHost;

    public override void OnFrameworkInitializationCompleted()
    {
        Resources["UiMap"] = new UiMapProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel();
            var window = new MainWindow { DataContext = vm };
            desktop.MainWindow = window;

            var options = DetectionOptions.Load();
            var templateRoot = Path.Combine(AppContext.BaseDirectory, options.TemplateDirectory);
            var outputRoot = Path.Combine(AppContext.BaseDirectory, options.OutputDirectory);
            Directory.CreateDirectory(outputRoot);
            IGameStateDetectorSet detectorSet = new ShadowverseDetectorSet(templateRoot);
#if WINDOWS
            IScreenSource screenSource = new WinScreenSource(options.ProcessName);
#else
            IScreenSource screenSource = new DummyScreenSource();
#endif
            var publisher = new JsonStreamPublisher(outputRoot);
            _detectionHost = new DetectionHost(screenSource, detectorSet, publisher, templateRoot);
            try
            {
                _ = _detectionHost.StartAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Detector start failed: {ex.Message}");
            }

            desktop.ShutdownRequested += (_, _) => _detectionHost?.StopAsync().GetAwaiter().GetResult();
            desktop.Exit += (_, _) => _detectionHost?.StopAsync().GetAwaiter().GetResult();
        }

        base.OnFrameworkInitializationCompleted();
    }

}