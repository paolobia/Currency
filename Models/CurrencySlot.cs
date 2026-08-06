namespace Currency.Models;

public sealed class CurrencySlot
{
    public string Code { get; set; } = "EUR";

    /// <summary>Testo mostrato nella casella: buffer grezzo mentre la riga è attiva, valore formattato quando non lo è.</summary>
    public string Text { get; set; } = "";

    public decimal Amount { get; set; }
}
