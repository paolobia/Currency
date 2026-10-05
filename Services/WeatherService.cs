using System.Globalization;
using System.Text.Json;
using Currency.Models;

namespace Currency.Services;

public sealed class WeatherService(HttpClient http)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly Dictionary<string, (WeatherForecast Forecast, DateTime FetchedUtc)> cache = new();

    /// <summary>Previsione di oggi (codice meteo WMO, min/max °C) per una coordinata. Ritorna null se offline o in caso di errore.</summary>
    public async Task<WeatherForecast?> GetForecastAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var key = $"{latitude.ToString("F2", CultureInfo.InvariantCulture)},{longitude.ToString("F2", CultureInfo.InvariantCulture)}";
        if (cache.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.FetchedUtc < CacheDuration)
            return cached.Forecast;

        try
        {
            var url = "https://api.open-meteo.com/v1/forecast" +
                      $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                      $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                      "&daily=weather_code,temperature_2m_max,temperature_2m_min" +
                      "&forecast_days=1&timezone=auto";

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var json = await http.GetStringAsync(url, timeout.Token);
            var dto = JsonSerializer.Deserialize(json, AppJsonContext.Default.OpenMeteoResponse);
            var daily = dto?.Daily;
            if (daily is null || daily.WeatherCode.Count == 0 || daily.TemperatureMin.Count == 0 || daily.TemperatureMax.Count == 0)
                return null;

            var forecast = new WeatherForecast(daily.WeatherCode[0], daily.TemperatureMin[0], daily.TemperatureMax[0]);
            cache[key] = (forecast, DateTime.UtcNow);
            return forecast;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Icona emoji rappresentativa di un codice meteo WMO (usato dalla daily API di Open-Meteo).</summary>
    public static string IconFor(int weatherCode) => weatherCode switch
    {
        0 => "☀️",
        1 or 2 => "🌤️",
        3 => "☁️",
        45 or 48 => "🌫️",
        51 or 53 or 55 or 56 or 57 => "🌦️",
        61 or 63 or 65 or 66 or 67 => "🌧️",
        71 or 73 or 75 or 77 or 85 or 86 => "🌨️",
        80 or 81 or 82 => "🌦️",
        95 or 96 or 99 => "⛈️",
        _ => "❓",
    };
}
