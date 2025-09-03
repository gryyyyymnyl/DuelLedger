namespace DuelLedger.Core.Pipelines;

public interface IFrameSource<TFrame>
{
    bool TryGetFrame(out TFrame frame);
}
