using System.Text.Json.Serialization;

namespace Currency.Models;

public sealed class OpenMeteoResponse
{
    [JsonPropertyName("daily")]
    public OpenMeteoDaily? Daily { get; set; }
}

public sealed class OpenMeteoDaily
{
    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public List<double> TemperatureMax { get; set; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public List<double> TemperatureMin { get; set; } = [];
}
