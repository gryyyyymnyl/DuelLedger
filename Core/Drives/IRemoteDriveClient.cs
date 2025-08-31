using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DuelLedger.Core.Drives;

public sealed class RemoteEntry
{
    public string Path { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
}

public interface IRemoteDriveClient
{
    Task<IReadOnlyList<RemoteEntry>> GetManifestAsync(CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string path, CancellationToken ct = default);
}
