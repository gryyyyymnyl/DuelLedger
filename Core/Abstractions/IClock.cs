namespace DuelLedger.Core.Abstractions;

public interface IClock
{
    DateTimeOffset Now { get; }
}
