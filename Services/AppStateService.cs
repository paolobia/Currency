using System.Text.Json;
using Currency.Models;

namespace Currency.Services;

public sealed class AppStateService(
    LocalStorageService storage,
    ExchangeRateService exchangeRates)
{
    private const string SlotsKey = "currency.slots";
    private const string RatesKey = "currency.rates";

    private static readonly string[] DefaultCodes = ["EUR", "HUF", "AED", "THB", "USD"];

    public CurrencySlot[] Slots { get; } = DefaultCodes.Select(c => new CurrencySlot { Code = c }).ToArray();

    public int ActiveIndex { get; private set; }

    public Dictionary<string, decimal> Rates { get; private set; } = new();

    public DateTime? LastUpdatedUtc { get; private set; }

    public bool IsOffline { get; private set; }

    public bool IsInitialized { get; private set; }

    /// <summary>Vero quando il testo della riga attiva è un valore "ereditato" (seed iniziale, importo ricalcolato dopo un cambio riga/valuta) mai digitato dall'utente: la prossima cifra lo sostituisce invece di accodarsi.</summary>
    private bool activeIsFreshEntry;

    public event Action? OnChange;

    private void NotifyChanged() => OnChange?.Invoke();

    public async Task InitializeAsync()
    {
        await LoadSlotsAsync();
        await LoadRatesAsync();

        ActiveIndex = 0;
        Slots[0].Amount = 1m;
        Slots[0].Text = "1";
        activeIsFreshEntry = true;
        RecomputeOthers();
        IsInitialized = true;
        NotifyChanged();

        await RefreshRatesAsync();
    }

    public async Task RefreshRatesAsync()
    {
        var snapshot = await exchangeRates.FetchLatestAsync();
        if (snapshot is not null)
        {
            Rates = snapshot.Rates;
            LastUpdatedUtc = snapshot.LastUpdatedUtc;
            IsOffline = false;
            await SaveRatesAsync();
            RecomputeOthers();
        }
        else
        {
            IsOffline = true;
        }

        NotifyChanged();
    }

    public void SetActive(int index)
    {
        if (index == ActiveIndex)
            return;

        ActiveIndex = index;
        Slots[index].Text = NumberFormatter.ToEditable(Slots[index].Amount);
        activeIsFreshEntry = true;
        NotifyChanged();
    }

    public void AppendDigit(char digit)
    {
        var baseText = activeIsFreshEntry ? "" : Slots[ActiveIndex].Text;
        activeIsFreshEntry = false;
        SetActiveBuffer(baseText + digit);
    }

    public void AppendComma()
    {
        var baseText = activeIsFreshEntry ? "" : Slots[ActiveIndex].Text;
        activeIsFreshEntry = false;
        SetActiveBuffer(baseText + ",");
    }

    public void Backspace()
    {
        if (activeIsFreshEntry)
        {
            activeIsFreshEntry = false;
            SetActiveBuffer("");
            return;
        }

        var text = Slots[ActiveIndex].Text;
        if (text.Length > 0)
            SetActiveBuffer(text[..^1]);
    }

    public void ClearActive()
    {
        activeIsFreshEntry = false;
        SetActiveBuffer("");
    }

    private void SetActiveBuffer(string sanitizedRaw)
    {
        var active = Slots[ActiveIndex];
        active.Text = sanitizedRaw;
        active.Amount = NumberFormatter.ParseBuffer(sanitizedRaw);
        RecomputeOthers();
        NotifyChanged();
    }

    public async Task SetSlotCurrencyAsync(int index, string code)
    {
        Slots[index].Code = code;
        if (index == ActiveIndex)
        {
            activeIsFreshEntry = true;
            RecomputeOthers();
        }
        else
        {
            Slots[index].Amount = ConvertAmount(Slots[ActiveIndex].Amount, Slots[ActiveIndex].Code, code);
            Slots[index].Text = NumberFormatter.FormatFixed(Slots[index].Amount);
        }

        await SaveSlotsAsync();
        NotifyChanged();
    }

    public decimal RateFor(string code) => Rates.GetValueOrDefault(code, 0m);

    public bool HasRateFor(string code) => Rates.ContainsKey(code);

    public decimal ConvertAmount(decimal amount, string fromCode, string toCode)
    {
        if (fromCode == toCode)
            return amount;

        var rateFrom = RateFor(fromCode);
        var rateTo = RateFor(toCode);
        if (rateFrom == 0 || rateTo == 0)
            return 0m;

        var usd = amount / rateFrom;
        return usd * rateTo;
    }

    private void RecomputeOthers()
    {
        var active = Slots[ActiveIndex];
        for (var i = 0; i < Slots.Length; i++)
        {
            if (i == ActiveIndex)
                continue;

            Slots[i].Amount = ConvertAmount(active.Amount, active.Code, Slots[i].Code);
            Slots[i].Text = NumberFormatter.FormatFixed(Slots[i].Amount);
        }
    }

    private async Task LoadSlotsAsync()
    {
        var json = await storage.GetItemAsync(SlotsKey);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var codes = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListString);
            if (codes is { Count: 5 })
            {
                for (var i = 0; i < 5; i++)
                    Slots[i].Code = codes[i];
            }
        }
        catch
        {
            // dati salvati corrotti: restano i default
        }
    }

    private async Task LoadRatesAsync()
    {
        var json = await storage.GetItemAsync(RatesKey);
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var snapshot = JsonSerializer.Deserialize(json, AppJsonContext.Default.RatesSnapshot);
            if (snapshot is { Rates.Count: > 0 })
            {
                Rates = snapshot.Rates;
                LastUpdatedUtc = snapshot.LastUpdatedUtc;
                IsOffline = true; // dati da cache finché non arriva una risposta di rete fresca
            }
        }
        catch
        {
            // dati salvati corrotti: si riparte da un dizionario vuoto, verrà popolato dal refresh
        }
    }

    private Task SaveSlotsAsync()
    {
        var codes = Slots.Select(s => s.Code).ToList();
        var json = JsonSerializer.Serialize(codes, AppJsonContext.Default.ListString);
        return storage.SetItemAsync(SlotsKey, json);
    }

    private Task SaveRatesAsync()
    {
        var snapshot = new RatesSnapshot { BaseCode = "USD", Rates = Rates, LastUpdatedUtc = LastUpdatedUtc ?? DateTime.UtcNow };
        var json = JsonSerializer.Serialize(snapshot, AppJsonContext.Default.RatesSnapshot);
        return storage.SetItemAsync(RatesKey, json);
    }
}
