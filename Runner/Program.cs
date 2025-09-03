using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DuelLedger.Core;
using DuelLedger.Detectors.Shadowverse;
using DuelLedger.Publishers;
using DuelLedger.Vision;
using DuelLedger.Core.Abstractions;
using DuelLedger.Infra.Templates;
using DuelLedger.Infra.Drives;
using DuelLedger.Infra.Config;
using OpenCvSharp;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        var cfgProvider = AppConfigProvider.LoadAsync("appsettings.json", "remote.json").GetAwaiter().GetResult();
        var resolver = new TemplatePathResolver(cfgProvider);
        var drive = new HttpStaticClient(cfgProvider);
        var sync = new TemplateSyncService(cfgProvider, resolver, drive);
        await sync.SyncAsync("Shadowverse");
        var templateRoot = resolver.Get("Shadowverse");
        if (args.Contains("--dry-run"))
        {
            Directory.CreateDirectory(templateRoot);
            var dummyTplPath = Path.Combine(templateRoot, "format_dummy.png");
            if (!File.Exists(dummyTplPath))
            {
                using var tpl = new Mat(new Size(50, 50), MatType.CV_8UC3, Scalar.White);
                Cv2.ImWrite(dummyTplPath, tpl);
            }
        }
        cfgProvider.Value.Games.TryGetValue("Shadowverse", out var gameCfg);
        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);
        var publisher = new JsonStreamPublisher(outDir);
        Console.WriteLine($"TemplateRoot: {templateRoot}");

        IGameStateDetectorSet setForManager;
        try
        {
            setForManager = new ShadowverseDetectorSet(templateRoot, gameCfg?.Keys ?? new Dictionary<string, string>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Shadowverse detectors: {ex.Message}");
            setForManager = new EmptyDetectorSet();
        }

        IScreenSource screenSource = args.Contains("--dry-run")
            ? new DummySequenceSource()
            : CreateScreenSource(setForManager.ProcessName);

        var manager = new GameStateManager(setForManager, screenSource, publisher);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        Console.WriteLine("DuelLedger Runner started. Press Ctrl+C to exit.");

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                manager.Update();
                await Task.Delay(200, cts.Token);
            }
        }
        catch (TaskCanceledException)
        {
            // graceful shutdown
        }

        if (args.Contains("--dry-run"))
        {
            var now = DateTimeOffset.UtcNow;
            publisher.PublishFinal(new MatchSummary(0, 0, 0, TurnOrder.Unknown, MatchResult.Unknown, now, now));
            Console.WriteLine($"Dummy match summary written to {Path.Combine(outDir, "matches")}");
        }

        return 0;
    }

    private static IScreenSource CreateScreenSource(string processName)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var type = Type.GetType("DuelLedger.Vision.Windows.WinScreenSource, DuelLedger.Vision.Windows");
                if (type != null)
                    return (IScreenSource)Activator.CreateInstance(type, processName)!;
            }
            catch { }
        }
        Console.WriteLine("No native screen source available; using dummy source.");
        return new DummyScreenSource();
    }

    private sealed class DummyScreenSource : IScreenSource
    {
        public bool TryCapture(out Mat frame)
        {
            frame = null!;
            return false;
        }
    }

    private sealed class DummySequenceSource : IScreenSource
    {
        public bool TryCapture(out Mat frame)
        {
            frame = new Mat(new Size(100, 100), MatType.CV_8UC3, Scalar.Black);
            Console.WriteLine("DummySequenceSource provided frame.");
            return true;
        }
    }

    private sealed class EmptyDetectorSet : IGameStateDetectorSet
    {
        public string GameName => "Shadowverse";
        public string ProcessName => string.Empty;
        public List<IStateDetector> CreateDetectors() => new();
    }
}
