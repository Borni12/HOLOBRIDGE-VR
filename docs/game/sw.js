// HoloBridge Metaverse - Cache-First Service Worker
// Enables Unlimited Opens with Zero Netlify Bandwidth Consumption
const CACHE_NAME = 'holobridge-v1-cache';

const IMMUTABLE_EXTENSIONS = [
  '.unityweb',
  '.wasm',
  '.data',
  '.mp4',
  '.png',
  '.ico'
];

self.addEventListener('install', (event) => {
  console.log('[HoloBridge SW] Installed. Activating immediately...');
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k))
      );
    }).then(() => self.clients.claim())
  );
  console.log('[HoloBridge SW] Active & claiming clients for 0-bandwidth cache.');
});

self.addEventListener('fetch', (event) => {
  const requestUrl = new URL(event.request.url);

  // Never cache external WebSocket / MQTT or API calls
  if (requestUrl.protocol === 'ws:' || requestUrl.protocol === 'wss:' || requestUrl.hostname !== self.location.hostname) {
    return;
  }

  // Cache-First strategy for heavy game assets (.unityweb, .data, .wasm, .mp4)
  const isHeavyAsset = IMMUTABLE_EXTENSIONS.some(ext => requestUrl.pathname.endsWith(ext));

  if (isHeavyAsset) {
    event.respondWith(
      caches.match(event.request).then((cachedResponse) => {
        if (cachedResponse) {
          // Serve from local disk/browser cache immediately (0 Netlify bandwidth!)
          return cachedResponse;
        }
        return fetch(event.request).then((networkResponse) => {
          if (networkResponse && networkResponse.status === 200) {
            const copy = networkResponse.clone();
            caches.open(CACHE_NAME).then((cache) => {
              cache.put(event.request, copy);
            });
          }
          return networkResponse;
        });
      })
    );
    return;
  }

  // Network-first with cache fallback for index.html
  event.respondWith(
    fetch(event.request).then((networkResponse) => {
      if (networkResponse && networkResponse.status === 200) {
        const copy = networkResponse.clone();
        caches.open(CACHE_NAME).then((cache) => {
          cache.put(event.request, copy);
        });
      }
      return networkResponse;
    }).catch(() => {
      return caches.match(event.request);
    })
  );
});
