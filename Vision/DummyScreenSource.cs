using OpenCvSharp;

namespace DuelLedger.Vision;

/// <summary>
/// Placeholder screen source used on non-Windows platforms.
/// </summary>
public sealed class DummyScreenSource : IScreenSource
{
    public bool TryCapture(out Mat frame)
    {
        frame = null!;
        return false;
    }
}
