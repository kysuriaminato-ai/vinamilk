/**
 * Vinamilk HRM Enterprise Service Worker v1.0
 * PWA Offline Support & Cache Strategy
 * 
 * SECURITY & PRIVACY CONSTRAINT:
 * Tuyệt đối KHÔNG cache dữ liệu lương, thưởng và thông tin nhạy cảm trên thiết bị!
 */

const CACHE_NAME = 'vnm-hrm-static-v1';
const STATIC_ASSETS = [
  '/',
  '/self-service',
  '/tuyen-dung',
  '/manifest.json'
];

// 1. Install Event: Cache App Shell
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(STATIC_ASSETS);
    })
  );
  self.skipWaiting();
});

// 2. Activate Event: Clean Old Caches
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
      );
    })
  );
  self.clients.claim();
});

// 3. Fetch Event: Intelligent Strategy
self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);

  // RÀNG BUỘC BẢO MẬT: Mọi request liên quan đến lương (/payroll, /salary-review, /api/) KHÔNG ĐƯỢC CACHE!
  const isSensitivePayrollData = 
    url.pathname.startsWith('/payroll') ||
    url.pathname.startsWith('/salary-review') ||
    url.pathname.startsWith('/api/') ||
    url.pathname.startsWith('/login') ||
    url.pathname.startsWith('/logout');

  if (isSensitivePayrollData) {
    // Luôn luôn gọi Network-Only, không chạm vào CacheStorage
    event.respondWith(fetch(event.request));
    return;
  }

  // Đối với static assets có hash (/dist/): Stale-While-Revalidate hoặc Cache-First
  if (url.pathname.startsWith('/dist/')) {
    event.respondWith(
      caches.match(event.request).then((cached) => {
        if (cached) return cached;
        return fetch(event.request).then((response) => {
          if (response.status === 200) {
            const clone = response.clone();
            caches.open(CACHE_NAME).then((cache) => cache.put(event.request, clone));
          }
          return response;
        });
      })
    );
    return;
  }

  // Network-First với Offline fallback cho các trang công khai
  event.respondWith(
    fetch(event.request)
      .then((response) => {
        return response;
      })
      .catch(() => {
        return caches.match(event.request).then((cached) => {
          if (cached) return cached;
          return caches.match('/self-service');
        });
      })
  );
});