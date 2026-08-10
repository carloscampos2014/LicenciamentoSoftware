// Registrar Service Worker para PWA
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/service-worker.js')
        .then(function(reg) { console.log('SW registrado:', reg.scope); })
        .catch(function(err) { console.warn('SW falhou:', err); });
}
