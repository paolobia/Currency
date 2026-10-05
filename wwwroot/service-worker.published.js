// Caching-first service worker per la build pubblicata.
// self.assetsManifest viene iniettato da service-worker-assets.js,
// generato automaticamente da `dotnet publish` (vedi Currency.csproj).

self.importScripts('./service-worker-assets.js');

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// Sostituisce le fetch verso l'origine con richieste "no-cors" quando necessario (CDN/API esterne).
async function onInstall(event) {
    console.info('Service worker: Install');

    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        // Niente `integrity`: il workflow di deploy riscrive <base href> in index.html dopo la publish,
        // quindi l'hash nel manifest non corrisponde più e cache.addAll() fallirebbe (SW mai installato → niente offline).
        .map(asset => new Request(asset.url, { cache: 'no-cache' }));

    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // Le richieste ai tassi di cambio (open.er-api.com) vanno sempre in rete: mai dalla cache.
        const shouldServeIndexHtml = event.request.mode === 'navigate';

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    if (cachedResponse)
        return cachedResponse;

    try {
        return await fetch(event.request);
    } catch {
        // Offline: risposta d'errore pulita invece di una promise rifiutata (l'app gestisce già il fallimento).
        return new Response('', { status: 503, statusText: 'Offline' });
    }
}

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
