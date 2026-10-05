using System.Text.Json;
using Currency.Models;

namespace Currency.Services;

public sealed class ExchangeRateService(HttpClient http)
{
    private const string ApiUrl = "https://open.er-api.com/v6/latest/USD";

    /// <summary>Scarica i tassi di cambio con base USD per tutte le valute. Ritorna null se offline o in caso di errore.</summary>
    public async Task<RatesSnapshot?> FetchLatestAsync(CancellationToken ct = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var json = await http.GetStringAsync(ApiUrl, timeout.Token);
            var dto = JsonSerializer.Deserialize(json, AppJsonContext.Default.OpenErApiResponse);
            if (dto is null || dto.Result != "success" || dto.Rates.Count == 0)
                return null;

            return new RatesSnapshot
            {
                BaseCode = dto.BaseCode,
                Rates = dto.Rates,
                LastUpdatedUtc = DateTime.UtcNow,
            };
        }
        catch
        {
            return null;
        }
    }
}
