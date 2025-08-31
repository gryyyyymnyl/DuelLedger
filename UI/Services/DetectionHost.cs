using System;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core;
using DuelLedger.Vision;
using OpenCvSharp;
using System.Runtime.InteropServices;

namespace DuelLedger.UI.Services;

/// <summary>
/// Hosts the game state detection loop and manages its lifetime.
/// </summary>
public sealed class DetectionHost : IAsyncDisposable
{
    private readonly IGameStateDetectorSet _detectorSet;
    private readonly IScreenSource _screenSource;
    private readonly IMatchPublisher _publisher;

    private GameStateManager? _manager;
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private bool _started;

    public DetectionHost(IGameStateDetectorSet detectorSet, IScreenSource screenSource, IMatchPublisher publisher)
    {
        _detectorSet = detectorSet;
        _screenSource = screenSource;
        _publisher = publisher;
    }

    public async Task StartAsync(CancellationToken token)
    {
        if (_started) return;
        _started = true;

        try
        {
            // Preflight: OpenCV とアーキ情報
            try
            {
                var ver = Cv2.GetVersionString();
                Console.WriteLine($"OpenCV: {ver}, ProcArch: {RuntimeInformation.ProcessArchitecture}, Is64Bit:{Environment.Is64BitProcess}");
            }
            catch (Exception pre)
            {
                Console.WriteLine($"OpenCV preflight failed: {pre}");
            }

            _manager = new GameStateManager(_detectorSet, _screenSource, _publisher);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _worker = Task.Run(async () =>
            {
                try
                {
                    while (!_cts!.IsCancellationRequested)
                    {
                        _manager.Update();
                        await Task.Delay(200, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // graceful exit
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Detection loop error: {ex}");
                }
            }, CancellationToken.None);

            Console.WriteLine("Detector started");
        }
        catch (Exception ex)
        {
            // 内部例外とスタックまで出力
            Console.WriteLine($"Detection start failed: {ex}");
            if (ex.InnerException != null)
                Console.WriteLine($"Inner: {ex.InnerException}");
        }
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!_started) return;
        _started = false;

        Console.WriteLine("Detector stopping");
        try
        {
            _cts?.Cancel();
            if (_worker != null)
            {
                var completed = await Task.WhenAny(_worker, Task.Delay(3000));
                if (completed != _worker)
                {
                    Console.WriteLine("Detector stop timeout");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Detection stop failed: {ex.Message}");
        }
        finally
        {
            _cts?.Dispose();
            _manager = null;
            _worker = null;
            _cts = null;
            Console.WriteLine("Detector stopped");
            Console.WriteLine("Flush complete");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}

/// <summary>
/// Dummy screen source used on non-Windows platforms.
/// </summary>
internal sealed class DummyScreenSource : IScreenSource
{
    public bool TryCapture(out Mat frame)
    {
        frame = null!;
        return false;
    }
}
