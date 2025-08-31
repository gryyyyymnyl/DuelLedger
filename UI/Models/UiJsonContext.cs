using System.Collections.Generic;
using System.Text.Json.Serialization;
using DuelLedger.UI.Services;

namespace DuelLedger.UI.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(MatchSummaryDto))]
[JsonSerializable(typeof(Dictionary<string, UiMapItem>))]
internal partial class UiJsonContext : JsonSerializerContext
{
}

