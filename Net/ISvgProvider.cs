namespace DuelLedger.Net;

public interface ISvgProvider
{
    Task<string?> GetSvgAsync(int classId);
}
