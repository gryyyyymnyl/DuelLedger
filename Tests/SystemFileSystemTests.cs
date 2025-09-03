using DuelLedger.Core.Util;

namespace DuelLedger.Tests;

public class SystemFileSystemTests
{
    [Fact]
    public void OpenReadShared_AllowsDelete()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test.txt");
        File.WriteAllText(path, "hi");
        try
        {
            using var s = SystemFileSystem.Instance.OpenReadShared(path);
            File.Delete(path); // should not throw
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
