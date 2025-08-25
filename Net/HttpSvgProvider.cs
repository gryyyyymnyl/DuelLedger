using System.Net.Http;

namespace DuelLedger.Net;

public class HttpSvgProvider : ISvgProvider
{
    private readonly HttpClient _client = new(){Timeout = TimeSpan.FromSeconds(5)};
    private static readonly Dictionary<int,string> Urls = new()
    {
        {1,"https://shadowverse-wb.com/assets/images/common/common/class/class_elf.svg"},
        {2,"https://shadowverse-wb.com/assets/images/common/common/class/class_royal.svg"},
        {3,"https://shadowverse-wb.com/assets/images/common/common/class/class_witch.svg"},
        {4,"https://shadowverse-wb.com/assets/images/common/common/class/class_dragon.svg"},
        {5,"https://shadowverse-wb.com/assets/images/common/common/class/class_nightmare.svg"},
        {6,"https://shadowverse-wb.com/assets/images/common/common/class/class_bishop.svg"},
        {7,"https://shadowverse-wb.com/assets/images/common/common/class/class_nemesis.svg"}
    };

    public async Task<string?> GetSvgAsync(int classId)
    {
        if (!Urls.TryGetValue(classId, out var url)) return null;
        try
        {
            return await _client.GetStringAsync(url);
        }
        catch
        {
            return null;
        }
    }
}
