using System.Collections.Generic;
using System.Text.Json.Serialization;
using DuelLedger.Core.Drives;

namespace DuelLedger.Infra.Drives;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<RemoteEntry>))]
internal partial class RemoteEntryJsonContext : JsonSerializerContext
{
}

