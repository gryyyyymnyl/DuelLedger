namespace DuelLedger.Core.Util;

using System;
using System.IO;
using DuelLedger.Core.Abstractions;

public sealed class SystemFileSystem : IFileSystem
{
    public static SystemFileSystem Instance { get; } = new();
    private SystemFileSystem() { }

    public Stream OpenReadShared(string path)
        => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    public void WriteAtomic(string path, byte[] data)
    {
        var tmp = path + $".{Environment.ProcessId}.tmp";
        using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            fs.Write(data, 0, data.Length);
            fs.Flush(true);
        }
        try
        {
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
            }
            else
            {
                File.Replace(tmp, path, destinationBackupFileName: null);
            }
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    public void WriteAllText(string path, string content)
        => File.WriteAllText(path, content);

    public void EnsureDirectory(string path)
        => Directory.CreateDirectory(path);
}
