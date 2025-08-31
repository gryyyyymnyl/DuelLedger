using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core;
using DuelLedger.Core.Config;
using DuelLedger.Core.Templates;
using DuelLedger.Infra.Templates;
using DuelLedger.Detectors.Shadowverse;
using DuelLedger.Publishers;
using DuelLedger.Vision;
using DuelLedger.Contracts;
using OpenCvSharp;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        var config = ConfigLoader.Load("appsettings.json");
        ITemplatePathResolver resolver = new TemplatePathResolver(config);
        var templateRoot = resolver.Get("Shadowverse");

        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);
        var publisher = new JsonStreamPublisher(outDir);

        IGameStateDetectorSet setForManager;
        try
        {
            setForManager = new ShadowverseDetectorSet(templateRoot);
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
            frame = null!;
            return false;
        }
    }

    private sealed class EmptyDetectorSet : IGameStateDetectorSet
    {
        public string GameName => "Shadowverse";
        public string ProcessName => string.Empty;
        public List<IStateDetector> CreateDetectors() => new();
    }
}
