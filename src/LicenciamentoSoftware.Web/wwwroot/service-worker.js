// Service Worker do LicenseManager PWA
// Estratégia: cache-first para assets estáticos do Blazor WASM
// A API (/api/*) sempre vai para a rede — sem modo offline para dados

const CACHE_NAME = 'licensemanager-v1';

// Assets que sempre ficam em cache (shell do app)
const SHELL_ASSETS = [
  '/',
  '/index.html',
  '/css/app.css',
  '/css/bootstrap.min.css',
  '/js/bootstrap.bundle.min.js',
  '/manifest.webmanifest',
  '/icon-192.png',
  '/icon-512.png',
  '/favicon.png',
];

// Instalar: pré-cachear o shell
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(cache => cache.addAll(SHELL_ASSETS))
  );
  self.skipWaiting();
});

// Ativar: remover caches antigos
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys =>
      Promise.all(
        keys
          .filter(key => key !== CACHE_NAME)
          .map(key => caches.delete(key))
      )
    )
  );
  self.clients.claim();
});

// Fetch: estratégias por tipo de request
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);

  // Requisições à API e ao BFF — sempre rede, sem cache
  if (url.pathname.startsWith('/api/') ||
      url.pathname.startsWith('/auth/') ||
      url.pathname.startsWith('/bff/')) {
    return; // deixa passar normalmente
  }

  // Framework Blazor WASM (_framework/) — cache-first
  if (url.pathname.startsWith('/_framework/')) {
    event.respondWith(
      caches.match(event.request).then(cached => {
        if (cached) return cached;
        return fetch(event.request).then(response => {
          if (response.ok) {
            const clone = response.clone();
            caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
          }
          return response;
        });
      })
    );
    return;
  }

  // Assets estáticos — cache-first com fallback para rede
  event.respondWith(
    caches.match(event.request).then(cached => {
      if (cached) return cached;

      return fetch(event.request).then(response => {
        // Cachear apenas respostas bem-sucedidas de assets estáticos
        if (response.ok && event.request.method === 'GET') {
          const clone = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
        }
        return response;
      }).catch(() => {
        // Offline e não está em cache — retornar o index.html para SPA routing
        if (event.request.mode === 'navigate') {
          return caches.match('/index.html');
        }
      });
    })
  );
});
