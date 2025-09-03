using System.IO;

namespace DuelLedger.Core.Abstractions;

public interface IFileSystem
{
    Stream OpenReadShared(string path);
    void WriteAtomic(string path, byte[] data);
    void WriteAllText(string path, string content);
    void EnsureDirectory(string path);
}
