using System.Text.Json.Serialization;
using Currency.Models;

namespace Currency.Services;

[JsonSerializable(typeof(OpenErApiResponse))]
[JsonSerializable(typeof(RatesSnapshot))]
[JsonSerializable(typeof(List<string>))]
internal partial class AppJsonContext : JsonSerializerContext;
