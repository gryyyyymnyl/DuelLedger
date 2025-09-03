using Avalonia;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DuelLedger.Core;
using DuelLedger.Games.Shadowverse;
using DuelLedger.Publishers;
using DuelLedger.Vision;
using DuelLedger.Core.Abstractions;
using DuelLedger.Infra.Templates;
using DuelLedger.Infra.Drives;
using DuelLedger.Infra.Config;
using OpenCvSharp;

namespace DuelLedger.UI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--dry-run"))
        {
            RunDryRun();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void RunDryRun()
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        var cfgProvider = AppConfigProvider.LoadAsync("appsettings.json", "remote.json").GetAwaiter().GetResult();
        var resolver = new TemplatePathResolver(cfgProvider);
        var drive = new HttpStaticClient(cfgProvider);
        var sync = new TemplateSyncService(cfgProvider, resolver, drive);
        sync.SyncAsync("Shadowverse").GetAwaiter().GetResult();
        var templateRoot = resolver.Get("Shadowverse");

        Directory.CreateDirectory(templateRoot);
        var dummyTplPath = Path.Combine(templateRoot, "format_dummy.png");
        if (!File.Exists(dummyTplPath))
        {
            using var tpl = new Mat(new OpenCvSharp.Size(50, 50), MatType.CV_8UC3, Scalar.White);
            Cv2.ImWrite(dummyTplPath, tpl);
        }

        cfgProvider.Value.Games.TryGetValue("Shadowverse", out var gameCfg);
        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);
        var publisher = new JsonStreamPublisher(outDir);

        IGameStateDetectorSet setForManager = new ShadowverseDetectorSet(templateRoot, gameCfg?.Keys ?? new Dictionary<string, string>());
        IScreenSource screenSource = new DummySequenceSource();
        var manager = new GameStateManager(setForManager, screenSource, publisher);
        manager.Update();

        var now = DateTimeOffset.UtcNow;
        publisher.PublishFinal(new MatchSummary(0, 0, 0, TurnOrder.Unknown, MatchResult.Unknown, now, now));
    }

    private sealed class DummySequenceSource : IScreenSource
    {
        public bool TryCapture(out Mat frame)
        {
            frame = new Mat(new OpenCvSharp.Size(100, 100), MatType.CV_8UC3, Scalar.Black);
            Console.WriteLine("DummySequenceSource provided frame.");
            return true;
        }
    }
}

