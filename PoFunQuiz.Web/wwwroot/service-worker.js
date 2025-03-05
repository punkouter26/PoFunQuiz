// Cache Names
const CACHE_NAME = 'pofunquiz-cache-v1';
const DYNAMIC_CACHE = 'pofunquiz-dynamic-cache-v1';

// Assets to cache immediately
const ASSETS_TO_CACHE = [
  './',
  './index.html',
  './offline.html',
  './app.css',
  './manifest.webmanifest',
  './favicon.png',
  './images/icon-192.png',
  './images/icon-512.png',
  './images/icon-maskable-192.png',
  './images/icon-maskable-512.png',
  './_framework/blazor.webassembly.js',
  './_framework/dotnet.js'
];

// Install event - cache the initial assets
self.addEventListener('install', (event) => {
  console.log('[Service Worker] Installing...');
  
  // Skip waiting forces the waiting service worker to become the active service worker
  self.skipWaiting();
  
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        console.log('[Service Worker] Caching app assets');
        return cache.addAll(ASSETS_TO_CACHE);
      })
  );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
  console.log('[Service Worker] Activating...');
  
  event.waitUntil(
    caches.keys()
      .then((keyList) => {
        return Promise.all(keyList.map((key) => {
          if (key !== CACHE_NAME && key !== DYNAMIC_CACHE) {
            console.log('[Service Worker] Removing old cache', key);
            return caches.delete(key);
          }
        }));
      })
  );
  
  // Claim control immediately
  return self.clients.claim();
});

// Fetch event - handle network requests with cache-first strategy
self.addEventListener('fetch', (event) => {
  console.log('[Service Worker] Fetching resource: ' + event.request.url);
  
  // Skip non-GET requests
  if (event.request.method !== 'GET') return;
  
  // Skip browser-sync and websocket requests
  if (event.request.url.includes('browser-sync') || 
      event.request.url.includes('ws:') ||
      event.request.url.includes('_framework/blazor-hotreload') ||
      event.request.url.includes('_blazor')) {
    return;
  }
  
  // Check if the request is for a page (HTML navigation)
  const isNavigationRequest = event.request.mode === 'navigate';
  
  // API requests go network-first
  if (event.request.url.includes('/api/')) {
    event.respondWith(
      fetch(event.request)
        .then(response => {
          // Clone response to store in cache
          const clonedResponse = response.clone();
          
          // Open dynamic cache and store response
          caches.open(DYNAMIC_CACHE)
            .then(cache => {
              cache.put(event.request, clonedResponse);
            });
            
          return response;
        })
        .catch(() => {
          // If network fails, try from cache
          return caches.match(event.request)
            .then(cachedResponse => {
              // If we have a cached response, return it
              if (cachedResponse) {
                return cachedResponse;
              }
              
              // For navigation requests, return the offline page
              if (isNavigationRequest) {
                return caches.match('./offline.html');
              }
              
              // For other requests, just return a failure
              return new Response('Network error', {
                status: 408,
                headers: new Headers({ 'Content-Type': 'text/plain' }),
              });
            });
        })
    );
  } else {
    // For other assets use cache-first strategy
    event.respondWith(
      caches.match(event.request)
        .then((cachedResponse) => {
          // Return cached response if found
          if (cachedResponse) {
            return cachedResponse;
          }
          
          // Otherwise fetch from network
          return fetch(event.request)
            .then((networkResponse) => {
              // Check if we received a valid response
              if (!networkResponse || networkResponse.status !== 200 || networkResponse.type !== 'basic') {
                return networkResponse;
              }
              
              // Clone the response since we want to use it twice
              const responseToCache = networkResponse.clone();
              
              // Add to dynamic cache
              caches.open(DYNAMIC_CACHE)
                .then((cache) => {
                  cache.put(event.request, responseToCache);
                });
                
              return networkResponse;
            })
            .catch(() => {
              // For navigation requests, return the offline page when network fails
              if (isNavigationRequest) {
                return caches.match('./offline.html');
              }
              return new Response('Network error', {
                status: 408,
                headers: new Headers({ 'Content-Type': 'text/plain' }),
              });
            });
        })
    );
  }
});

// Handle messages from clients (like the main app)
self.addEventListener('message', (event) => {
  if (event.data === 'SKIP_WAITING') {
    self.skipWaiting();
  }
});