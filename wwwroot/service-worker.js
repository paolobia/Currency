// In sviluppo il service worker non fa cache: evita di dover invalidare
// continuamente durante il lavoro. In produzione viene sostituito da
// service-worker.published.js (vedi Currency.csproj).
self.addEventListener('fetch', () => { });
