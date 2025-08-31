using System.Text.Json.Serialization;
using DuelLedger.Contracts;

namespace DuelLedger.Publishers;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MatchSnapshot))]
[JsonSerializable(typeof(MatchSummary))]
internal partial class MatchJsonContext : JsonSerializerContext
{
}

