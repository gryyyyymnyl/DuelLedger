using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DuelLedger.Core.Templates;

public interface IRemoteDriveClient
{
    Task<IReadOnlyList<RemoteFile>> GetManifestAsync(CancellationToken ct);
    Task<Stream> DownloadAsync(string path, CancellationToken ct);
}

public sealed class RemoteFile
{
    public string Path { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
}
