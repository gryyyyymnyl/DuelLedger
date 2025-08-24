using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Core;
using DuelLedger.Vision;

namespace DuelLedger.UI.Services;

/// <summary>
/// Hosts the background detection loop.
/// </summary>
public sealed class DetectionHost
{
    private readonly GameStateManager _manager;
    private readonly IScreenSource _screenSource;
    private readonly IMatchPublisher _publisher;
    private readonly string _templateRoot;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public DetectionHost(IScreenSource screenSource, IGameStateDetectorSet detectorSet, IMatchPublisher publisher, string templateRoot)
    {
        _screenSource = screenSource;
        _publisher = publisher;
        _templateRoot = templateRoot;
        _manager = new GameStateManager(detectorSet, screenSource, publisher);
    }

    public Task StartAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromCanceled(token);

        _loop = Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    _manager.Update();
                    await Task.Delay(200, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // graceful
            }
        }, CancellationToken.None);
        Console.WriteLine($"Detector started templates={_templateRoot} source={_screenSource.GetType().Name}");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        if (_loop != null)
        {
            try { await _loop; } catch (OperationCanceledException) { }
        }
        _cts.Dispose();
        (_publisher as System.IDisposable)?.Dispose();
        Console.WriteLine("Detector stopped");
        Console.WriteLine("flush complete");
    }
}
