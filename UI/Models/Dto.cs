namespace DuelLedger.UI.Models;

using System.Text.Json.Serialization;

// Publisher(JSON)のキーに合わせる（StartedAt/EndedAt）
public sealed class MatchSummaryDto
{
    public int SelfClass { get; set; }
    public int OppClass { get; set; }
    public int Order { get; set; }
    public int Result { get; set; }

    [JsonPropertyName("StartedAt")] public DateTimeOffset StartedAt { get; set; }
    [JsonPropertyName("EndedAt")]   public DateTimeOffset EndedAt   { get; set; }
}

public sealed class MatchSnapshotDto
{
    public int SelfClass { get; set; }
    public int OppClass { get; set; }
    public int Order { get; set; }
    public int Result { get; set; }

    [JsonPropertyName("StartedAt")] public DateTimeOffset? StartedAt { get; set; }
    [JsonPropertyName("EndedAt")]   public DateTimeOffset? EndedAt   { get; set; }
}