namespace DuelLedger.Core.Util;

using DuelLedger.Core.Abstractions;

public sealed class SystemClock : IClock
{
    public static SystemClock Instance { get; } = new();
    private SystemClock() { }
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
