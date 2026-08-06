using System.Text.Json.Serialization;

namespace Currency.Models;

public sealed class OpenErApiResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = "";

    [JsonPropertyName("base_code")]
    public string BaseCode { get; set; } = "";

    [JsonPropertyName("time_last_update_utc")]
    public string TimeLastUpdateUtc { get; set; } = "";

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
