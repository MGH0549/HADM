const CACHE_NAME = 'hadm-static-v1';
const APP_SHELL = ['/offline.html', '/manifest.webmanifest', '/icons/hadm.svg'];
const STATIC_FILE = /\.(?:js|css|svg|png|jpg|jpeg|webp|ico|woff2?)$/i;

self.addEventListener('install', event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(APP_SHELL)));
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(key => key.startsWith('hadm-static-') && key !== CACHE_NAME).map(key => caches.delete(key))))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener('fetch', event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== self.location.origin || url.pathname.startsWith('/api/')) return;

  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).catch(() => caches.match('/offline.html')));
    return;
  }

  if (!STATIC_FILE.test(url.pathname) && url.pathname !== '/manifest.webmanifest') return;
  event.respondWith(
    caches.match(request).then(cached => cached || fetch(request).then(response => {
      if (response.ok && response.type === 'basic') {
        event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.put(request, response.clone())));
      }
      return response;
    })),
  );
});
