using DuelLedger.Vision;
using OpenCvSharp;

namespace DuelLedger.UI.Services;

internal sealed class DummyScreenSource : IScreenSource
{
    public bool TryCapture(out Mat frame)
    {
        frame = null!;
        return false;
    }
}
