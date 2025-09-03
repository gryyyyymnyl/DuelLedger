using DuelLedger.Core.Abstractions;

namespace DuelLedger.Core.Pipelines;

public interface ISnapshotPublisher
{
    Task PublishAsync(MatchSnapshot snapshot, CancellationToken ct = default);
}
