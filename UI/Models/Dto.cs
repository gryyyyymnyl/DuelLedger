namespace DuelLedger.UI.Models;

using System.Text.Json.Serialization;

// Publisher(JSON)のキーに合わせる（StartAt/EndAt）
public sealed class MatchSummaryDto
{
    public int Format { get; set; }
    public int SelfClass { get; set; }
    public int OppClass { get; set; }
    public int Order { get; set; }
    public int Result { get; set; }

    [JsonPropertyName("StartAt")] public DateTimeOffset StartAt { get; set; }
    [JsonPropertyName("EndAt")]   public DateTimeOffset EndAt   { get; set; }
}

public sealed class MatchSnapshotDto
{
    public int Format { get; set; }
    public int SelfClass { get; set; }
    public int OppClass { get; set; }
    public int Order { get; set; }
    public int Result { get; set; }

    [JsonPropertyName("StartAt")] public DateTimeOffset? StartAt { get; set; }
    [JsonPropertyName("EndAt")]   public DateTimeOffset? EndAt   { get; set; }
}