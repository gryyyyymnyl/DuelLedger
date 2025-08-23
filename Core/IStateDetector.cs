namespace DuelLedger.Core;

using OpenCvSharp;

public interface IStateDetector
{
    GameState State { get; }
    bool IsMatch(Mat screen, out double score, out OpenCvSharp.Point matchLocation);
}