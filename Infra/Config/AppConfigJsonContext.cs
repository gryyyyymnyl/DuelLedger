using System.Text.Json;
using System.Text.Json.Serialization;

namespace DuelLedger.Infra.Config;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext
{
}
