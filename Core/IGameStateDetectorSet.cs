namespace DuelLedger.Core;

using System.Collections.Generic;

public interface IGameStateDetectorSet
{
    string GameName { get; }
    string ProcessName { get; }
    List<IStateDetector> CreateDetectors();
}