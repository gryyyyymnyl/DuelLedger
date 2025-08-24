using System;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core;
using DuelLedger.Contracts;
using DuelLedger.Vision;

namespace DuelLedger.UI.Services;

/// <summary>
/// Hosts <see cref="GameStateManager"/> in the background and manages its lifecycle.
/// </summary>
public sealed class DetectionHost : IAsyncDisposable, IDisposable
{
    private readonly IMatchPublisher _publisher;
    private readonly IGameStateDetectorSet _detectorSet;
    private readonly IScreenSource _screenSource;
    private Task? _loop;
    private CancellationTokenSource? _cts;
    private bool _started;

    public DetectionHost(IMatchPublisher publisher, IGameStateDetectorSet detectorSet, IScreenSource screenSource)
    {
        _publisher = publisher;
        _detectorSet = detectorSet;
        _screenSource = screenSource;
    }

    /// <summary>Starts the detection loop if not already started.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started) return;
        _started = true;
        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var manager = new GameStateManager(_detectorSet, _screenSource, _publisher);
            _loop = Task.Run(async () =>
            {
                Console.WriteLine("Detector started");
                try
                {
                    while (!_cts!.IsCancellationRequested)
                    {
                        try { manager.Update(); }
                        catch (Exception ex) { Console.WriteLine($"[Detection] {ex.Message}"); }

                        try { await Task.Delay(200, _cts.Token); }
                        catch (TaskCanceledException) { break; }
                    }
                }
                finally
                {
                    Console.WriteLine("Detector stopped");
                }
            }, _cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DetectionHost] start failed: {ex.Message}");
        }
    }

    /// <summary>Signals the loop to stop and waits for completion.</summary>
    public async Task StopAsync()
    {
        if (!_started) return;
        Console.WriteLine("Detector stopping");
        _started = false;
        try
        {
            _cts?.Cancel();
            if (_loop != null)
            {
                var completed = await Task.WhenAny(_loop, Task.Delay(TimeSpan.FromSeconds(5)));
                if (completed != _loop)
                {
                    Console.WriteLine("[DetectionHost] stop timeout");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DetectionHost] stop error: {ex.Message}");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _loop = null;
            Console.WriteLine("Flush complete");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        Dispose();
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
