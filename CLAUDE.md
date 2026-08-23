# Currency

App Blazor WebAssembly (.NET 8, no ASP.NET host) per convertire valute in tempo reale — pensata per l'uso da smartphone (5 righe di valuta + tastierino numerico stile calcolatrice, vedi `wwwroot/screenshot.png` per uno screenshot aggiornato).

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
- **Services/WeatherService.cs** — fetch HTTP delle previsioni del giorno (codice meteo WMO + min/max °C) da `https://api.open-meteo.com/v1/forecast` (gratuita, no API key, CORS abilitato) per le coordinate della città rappresentativa della valuta; cache in-memory per coordinata (30 minuti) per non richiamare l'API ad ogni render; ritorna `null` in caso di errore, mai eccezioni verso il chiamante (stesso pattern di `ExchangeRateService`). `IconFor(weatherCode)` mappa il codice WMO a un'emoji.
- **Services/NumberFormatter.cs** — parsing/formattazione del buffer numerico con virgola come separatore decimale (formato IT).
- **Services/AppJsonContext.cs** — `JsonSerializerContext` source-generated per la (de)serializzazione AOT-friendly.
- **Data/CurrencyCatalog.cs** — elenco statico valute (codice, nome, codice paese ISO2 per la bandiera, fuso orario IANA rappresentativo, latitudine/longitudine della stessa città rappresentativa) usato dal picker, per le bandierine, per l'orologio locale e per il meteo in `CurrencyRow`.
- **Shared/** — componenti riutilizzabili: `CurrencyRow` (una riga bandiera+codice+input), `KeyPad` (tastierino C/cifre/virgola/backspace/refresh/tabellina), `CurrencyPickerDialog` (ricerca e scelta valuta per uno slot), `ConversionGridDialog` (tabellina 1-100 dalla valuta attiva a un'altra).
- **wwwroot/flags/** — 154 SVG da [flag-icons](https://github.com/lipis/flag-icons) (MIT), una per ogni paese usato in `CurrencyCatalog` + `xx.svg` di fallback. Sostituiscono le emoji bandiera: molti Android non le renderizzano (font di sistema senza glifi flag), le SVG invece sono identiche ovunque.
- **PWA**: `wwwroot/manifest.json`, `wwwroot/service-worker.js` (no-op, usato in dev) e `wwwroot/service-worker.published.js` (cache-first, sostituisce il primo nella build Release grazie a `<ServiceWorker>`/`<ServiceWorkerAssetsManifest>` in `Currency.csproj`). Solo `dotnet publish -c Release` genera `service-worker-assets.js` e attiva la cache offline — `dotnet run` in sviluppo usa il service worker no-op di proposito.

## Note e decisioni prese

- Le conversioni passano sempre per USD come valuta ponte (`ConvertAmount`: `amount / rateFrom * rateTo`), perché l'API restituisce tassi con base USD.
- `ConversionGridDialog`: la valuta di destinazione è "la prima riga diversa da quella attiva" (`Slots[ActiveIndex == 0 ? 1 : 0]`). Prima del fix mostrava `Slots[0]` fisso, quindi con la riga 0 (EUR) attiva di default all'avvio la tabellina si apriva come `EUR → EUR`, inutile — bug corretto il 2026-08-06.
- Verificato manualmente in browser (screenshot via Playwright): inserimento cifre + ricalcolo righe, apertura picker valuta, apertura tabellina — tutto funzionante, nessun problema noto aperto.
- `Currency.csproj` deve usare **slash** (`wwwroot/service-worker.js`), non backslash Windows, negli `Include`/`PublishedContent` dell'item `ServiceWorker` — su Linux i backslash non vengono risolti e la sostituzione silenziosamente non avviene.
- Il repository GitHub (`paolobia/Currency`) è **pubblico** e pubblicato via GitHub Pages: https://paolobia.github.io/Currency/ (deploy automatico ad ogni push su `main` tramite `.github/workflows/deploy-pages.yml`). Dopo ogni deploy si mantengono visibili solo l'ultima run Actions e l'ultimo deployment (preferenza permanente dell'utente, vedi memoria di sessione `keep-only-latest-run`): si elimina la run precedente (`gh api -X DELETE repos/{owner}/{repo}/actions/runs/{id}`) e il deployment precedente (prima `POST .../deployments/{id}/statuses -f state=inactive`, poi `DELETE .../deployments/{id}`).
- `Shared/CurrencyRow.razor`: l'`<input class="amount-input">` è `readonly` con `inputmode="none"` — di proposito, per impedire l'apertura della tastiera nativa su mobile quando si tocca la riga (l'input serve solo a mostrare il valore e attivare la riga per il tastierino custom `KeyPad`, tutta la digitazione passa da lì). Bug corretto il 2026-08-06: prima era editabile e su smartphone compariva la tastiera di sistema sopra al tastierino dell'app.
- `wwwroot/css/app.css`: `html, body` e `#app` hanno `overscroll-behavior-y: none` per disattivare il pull-to-refresh nativo del browser, che altrimenti spostava visibilmente la pagina verso il basso durante lo swipe (bug corretto il 2026-08-06).
- Ogni `CurrencyInfo` porta un `TimeZoneId` IANA rappresentativo del paese della valuta (per EUR: `Europe/Rome`, non un paese specifico). `Shared/CurrencyRow.razor` lo usa per mostrare l'ora locale (formato 24h, `HH:mm`) sotto al codice valuta, aggiornata con un `System.Threading.Timer` per-riga (tick ogni 10s, sufficiente per la precisione al minuto). Feature aggiunta il 2026-08-10.
- `Shared/CurrencyRow.razor`: tra il codice valuta e l'ora locale mostra icona meteo (emoji) e min/max °C del giorno per la città del `TimeZoneId` (stessa `Latitude`/`Longitude` in `CurrencyInfo`). Il fetch (`WeatherService.GetForecastAsync`) riparte da `OnParametersSet` confrontando il codice valuta corrente con l'ultimo per cui è stata scaricata la previsione, così si aggiorna quando cambia la valuta della riga (cambio riga attiva o scelta dal picker) senza richieste superflue ad ogni render. `code-col` è stato allargato (52px→64px) e il padding orizzontale di `amount-input` ridotto (12px→8px) per fare spazio alle righe aggiuntive. Feature aggiunta il 2026-08-23.

## Build/test

```bash
dotnet build   # 0 warning, 0 error attesi
```

Non ci sono test automatici nel progetto.
