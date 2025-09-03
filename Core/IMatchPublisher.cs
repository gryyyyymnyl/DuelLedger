using DuelLedger.Core.Abstractions;
namespace DuelLedger.Core;

public interface IMatchPublisher
{
    void PublishSnapshot(MatchSnapshot snapshot); // 進行中
    void PublishFinal(MatchSummary summary);      // 確定
}