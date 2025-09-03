namespace DuelLedger.Core.Pipelines;

public interface IDetector<TFrame>
{
    bool TryDetect(TFrame frame, out DetectionResult result);
}
