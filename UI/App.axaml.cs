using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DuelLedger.UI.Views;
using DuelLedger.UI.ViewModels;
using DuelLedger.UI.Services;
using DuelLedger.Core;
using DuelLedger.Publishers;
using DuelLedger.Detectors.Shadowverse;
using DuelLedger.Vision;
using DuelLedger.Infra.Templates;
using DuelLedger.Infra.Drives;
using DuelLedger.Core.Config;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
#if WINDOWS
using DuelLedger.Vision.Windows;
#endif

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
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        if (OperatingSystem.IsWindows())
        {
            var nativePath = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native");
            var current = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            Environment.SetEnvironmentVariable("PATH", nativePath + ";" + current);
        }
        
        var config = ConfigLoader.Load("appsettings.json");
        var resolver = new TemplatePathResolver(config);
        var drive = new HttpStaticClient(config.Assets.Remote!);
        var sync = new TemplateSyncService(config, resolver, drive);

        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);

        Resources["UiMap"] = new UiMapProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var reader = new MatchReaderService(outDir);
            var vm = new MainWindowViewModel(reader);
            var window = new MainWindow(vm);
            desktop.MainWindow = window;

            Dispatcher.UIThread.Post(async () =>
            {
                var keys = vm.History.SelectMany(h => new[] { h.SelfClass.ToString(), h.OppClass.ToString() });
                await SvgIconStore.Instance.WarmAsync(keys);
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    var progress = new Progress<double>(p => Dispatcher.UIThread.Post(() => vm.DownloadProgress = p));
                    Dispatcher.UIThread.Post(() => vm.IsDownloadingTemplates = true);
                    await sync.SyncAsync("Shadowverse", progress);
                    Dispatcher.UIThread.Post(() => { vm.IsDownloadingTemplates = false; vm.DownloadProgress = 0; });
                    var templateRoot = resolver.Get("Shadowverse");
                    config.Games.TryGetValue("Shadowverse", out var gameCfg);

                    IGameStateDetectorSet detectorSet;
                    try
                    {
                        detectorSet = new ShadowverseDetectorSet(templateRoot, gameCfg?.Keys ?? new Dictionary<string, string>());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to initialize Shadowverse detectors: {ex.Message}");
                        detectorSet = new EmptyDetectorSet();
                    }

                    IScreenSource screenSource;
#if WINDOWS
                    if (OperatingSystem.IsWindows())
                        screenSource = new WinScreenSource(detectorSet.ProcessName);
                    else
                        screenSource = new DummyScreenSource();
#else
                    screenSource = new DummyScreenSource();
#endif
                    var publisher = new JsonStreamPublisher(outDir);
                    _host = new DetectionHost(detectorSet, screenSource, publisher);
                    await _host.StartAsync(CancellationToken.None);

                    Console.WriteLine($"TemplateRoot: {templateRoot}");
                    Console.WriteLine($"OutputRoot: {outDir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Background init failed: {ex.Message}");
                }
            });

            desktop.ShutdownRequested += async (_, e) =>
            {
                e.Cancel = true;
                if (_host is not null)
                    await _host.StopAsync();
                desktop.Shutdown();
            };

            desktop.Exit += async (_, _) =>
            {
                if (_host is not null)
                    await _host.StopAsync();
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