using System.Threading;
using System.Threading.Tasks;

namespace DuelLedger.Core.Templates;

public interface ITemplateSyncService
{
    Task<TemplateSyncReport> SyncAsync(string gameName, CancellationToken ct);
}

public sealed class TemplateSyncReport
{
    public int Total { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
}
