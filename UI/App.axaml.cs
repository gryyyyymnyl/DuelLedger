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
using DuelLedger.Infra.Drives;
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
            var nativePath = AppDomain.CurrentDomain.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string
                             ?? Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native");
            var current = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            Environment.SetEnvironmentVariable("PATH", nativePath + ";" + current);
        }

        try
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "appsettings.json");
            if (!File.Exists(path))
                throw new InvalidOperationException($"Missing config: {path}");

            var json = File.ReadAllText(path);
            var raw = System.Text.Json.JsonSerializer.Deserialize(json, DuelLedger.Infra.Config.AppConfigJsonContext.Default.AppConfig)
                      ?? throw new InvalidOperationException("appsettings.json deserialization returned null");

            if (raw.Remote is null || string.IsNullOrWhiteSpace(raw.Remote.StaticBaseUrl))
                throw new InvalidOperationException("Remote.StaticBaseUrl is missing or empty in appsettings.json");

            _ = new HttpStaticClient(raw.Remote);

            var outDir = Path.Combine(baseDir, "out");
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
                        var templateRoot = Path.Combine(baseDir, "Templates", "shadowverse");
                        var keys = new Dictionary<string, string>
                        {
                            ["Format"] = "format*",
                            ["MatchStart"] = "matchStart*",
                            ["BattleOwn"] = "battleClassOwn*",
                            ["BattleEnemy"] = "battleClassEmy*",
                            ["Result"] = "result*"
                        };

                        IGameStateDetectorSet detectorSet;
                        try
                        {
                            detectorSet = new ShadowverseDetectorSet(templateRoot, keys);
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
                        Console.WriteLine($"Background init failed: {ex}");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Startup failed: {ex}");
            throw;
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