namespace Currency.Models;

public sealed record CurrencyInfo(string Code, string Name, string FlagCountry, string TimeZoneId, double Latitude, double Longitude);
