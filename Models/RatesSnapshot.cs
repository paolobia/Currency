namespace Currency.Models;

public sealed class RatesSnapshot
{
    public string BaseCode { get; set; } = "USD";
    public Dictionary<string, decimal> Rates { get; set; } = new();
    public DateTime LastUpdatedUtc { get; set; }
}
