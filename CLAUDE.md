# Currency

App Blazor WebAssembly (.NET 8, no ASP.NET host) per convertire valute in tempo reale — pensata per l'uso da smartphone (5 righe di valuta + tastierino numerico stile calcolatrice, vedi `dialog.png` per il mockup di riferimento).

## Come si avvia

```bash
dotnet run --urls http://0.0.0.0:5289
```

Poi apri `http://localhost:5289` nel browser. Nessun backend/API key richiesta lato nostro: i tassi arrivano da `https://open.er-api.com/v6/latest/USD` (gratuita, base USD).

## Architettura

- **Program.cs** — bootstrap WASM standard, registra i servizi in DI (`Scoped`).
- **Pages/Home.razor** — pagina unica: elenco righe valuta (`AppState.Slots`), status bar aggiornamento tassi, tastierino, e i due dialog (picker valuta, tabellina).
- **Services/AppStateService.cs** — cuore dell'app, stato condiviso via evento `OnChange`:
  - `Slots`: array fisso di 5 `CurrencySlot` (codice valuta + testo editabile + importo).
  - `ActiveIndex`: riga attualmente editabile dal tastierino.
  - Digitare sul tastierino modifica solo la riga attiva (`Text` grezzo); le altre 4 righe vengono ricalcolate (`RecomputeOthers`) convertendo sempre passando per USD come base comune.
  - Slot codici e ultimo snapshot tassi vengono persistiti in `localStorage` (via `LocalStorageService`) così l'app riparte offline con l'ultimo cambio noto (`IsOffline = true` finché non arriva un fetch fresco).
- **Services/ExchangeRateService.cs** — fetch HTTP dei tassi, ritorna `null` in caso di errore/offline (mai eccezioni verso il chiamante).
- **Services/NumberFormatter.cs** — parsing/formattazione del buffer numerico con virgola come separatore decimale (formato IT).
- **Services/AppJsonContext.cs** — `JsonSerializerContext` source-generated per la (de)serializzazione AOT-friendly.
- **Data/CurrencyCatalog.cs** — elenco statico valute (codice, nome, emoji bandiera) usato dal picker e per le bandierine.
- **Shared/** — componenti riutilizzabili: `CurrencyRow` (una riga bandiera+codice+input), `KeyPad` (tastierino C/cifre/virgola/backspace/refresh/tabellina), `CurrencyPickerDialog` (ricerca e scelta valuta per uno slot), `ConversionGridDialog` (tabellina 1-100 dalla valuta attiva a un'altra).

## Note e decisioni prese

- Le conversioni passano sempre per USD come valuta ponte (`ConvertAmount`: `amount / rateFrom * rateTo`), perché l'API restituisce tassi con base USD.
- `ConversionGridDialog`: la valuta di destinazione è "la prima riga diversa da quella attiva" (`Slots[ActiveIndex == 0 ? 1 : 0]`). Prima del fix mostrava `Slots[0]` fisso, quindi con la riga 0 (EUR) attiva di default all'avvio la tabellina si apriva come `EUR → EUR`, inutile — bug corretto il 2026-08-06.
- Verificato manualmente in browser (screenshot via Playwright): inserimento cifre + ricalcolo righe, apertura picker valuta, apertura tabellina — tutto funzionante, nessun problema noto aperto.

## Build/test

```bash
dotnet build   # 0 warning, 0 error attesi
```

Non ci sono test automatici nel progetto.
