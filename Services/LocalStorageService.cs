using Microsoft.JSInterop;

namespace Currency.Services;

public sealed class LocalStorageService(IJSRuntime js)
{
    public async Task<string?> GetItemAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetItemAsync(string key, string value)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch
        {
            // Storage non disponibile (es. modalità privata): l'app continua a funzionare solo in memoria.
        }
    }
}
