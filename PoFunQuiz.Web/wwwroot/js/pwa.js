// Register service worker for PWA functionality
if ('serviceWorker' in navigator) {
    window.addEventListener('load', function() {
        navigator.serviceWorker.register('./service-worker.js')
            .then(function(registration) {
                console.log('ServiceWorker registration successful with scope: ', registration.scope);
                
                // Check for updates
                registration.addEventListener('updatefound', () => {
                    const newWorker = registration.installing;
                    newWorker.addEventListener('statechange', () => {
                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                            console.log('New service worker installed, update available');
                            // You can notify the user about the update here
                        }
                    });
                });
            })
            .catch(function(err) {
                console.log('ServiceWorker registration failed: ', err);
            });
    });

    // Handle service worker updates
    let refreshing = false;
    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (!refreshing) {
            refreshing = true;
            window.location.reload();
        }
    });
}

// Add to home screen functionality detection
window.addEventListener('beforeinstallprompt', (e) => {
    // Prevent Chrome 67 and earlier from automatically showing the prompt
    e.preventDefault();
    
    // Store the event for later use
    window.deferredPrompt = e;
    
    // Optionally, show your own "Add to Home Screen" UI element
    console.log('App can be installed. You can add UI to show this.');
});