// Handle element focusing for keyboard input
window.focusElement = (element) => {
    console.log("Attempting to focus element", element);
    if (element) {
        try {
            element.focus();
            console.log("Element focused successfully");
            
            // Add a click event to ensure focus is maintained
            element.addEventListener('click', function() {
                this.focus();
                console.log("Element refocused after click");
            });
            
            return true;
        } catch (e) {
            console.error("Error focusing element:", e);
            return false;
        }
    } else {
        console.warn("Element not found for focusing");
        return false;
    }
};

// Handle element focusing for keyboard input
window.focusGameContainer = function (elementId) {
    console.log("Attempting to focus element", elementId);
    const element = document.getElementById(elementId);
    if (element) {
        try {
            element.focus();
            console.log("Element focused successfully");
            
            // Add a click event to ensure focus is maintained
            element.addEventListener('click', function() {
                this.focus();
                console.log("Element refocused after click");
            });
            
            return true;
        } catch (e) {
            console.error("Error focusing element:", e);
            return false;
        }
    } else {
        console.warn("Element not found for focusing");
        return false;
    }
};

// Helper function to suppress default keyboard events when needed
window.suppressKeyboardEvent = (event, keys) => {
    console.log("Key pressed:", event.key);
    if (keys.includes(event.key)) {
        event.preventDefault();
        console.log("Key event suppressed:", event.key);
        return false;
    }
    return true;
};

// Store a reference to the Blazor connection
window._blazorConnection = null;

// Initialize Blazor connection reference
window.initializeBlazorConnection = () => {
    try {
        // Wait for Blazor to be fully initialized
        if (Blazor && Blazor._internal) {
            window._blazorConnection = Blazor._internal.navigationManager.connect;
            console.log("Blazor connection reference initialized successfully");
            return true;
        } else {
            console.warn("Blazor not fully initialized yet");
            return false;
        }
    } catch (e) {
        console.error("Error initializing Blazor connection reference:", e);
        return false;
    }
};

// Debug connection state
window.debugConnection = () => {
    console.log("Connection state debug info:");
    
    try {
        if (window._blazorConnection && window._blazorConnection.connection) {
            console.log("- readyState:", window._blazorConnection.connection.connectionState);
            console.log("- transport:", window._blazorConnection.connection.transport ? 
                window._blazorConnection.connection.transport.name : "No transport");
        } else {
            console.log("- readyState: No connection");
            console.log("- transport: No transport");
        }
    } catch (e) {
        console.error("Error accessing connection properties:", e);
        console.log("- readyState: Error accessing");
        console.log("- transport: Error accessing");
    }
    
    return true;
};

// Mobile optimizations
window.initMobileOptimizations = () => {
    // Check if we're on a mobile device
    const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
        (window.innerWidth <= 768);
    
    if (isMobile) {
        console.log("Mobile device detected, applying optimizations");
        
        // Add viewport meta tag to prevent zooming
        const viewportMeta = document.querySelector('meta[name="viewport"]');
        if (viewportMeta) {
            viewportMeta.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';
        }
        
        // Add class to body for mobile-specific styling
        document.body.classList.add('mobile-device');
    }
    
    return isMobile;
};

// Browser logging integration: send console logs, errors, and network events to the server's debug endpoints.
// Uses navigator.sendBeacon when available, falls back to fetch. Limits captured response bodies to 2KB.
(function() {
    const post = (url, payload) => {
        try {
            const body = JSON.stringify(payload);
            if (navigator.sendBeacon) {
                // sendBeacon expects a Blob or string-like, convert to Blob for content-type
                const blob = new Blob([body], { type: 'application/json' });
                navigator.sendBeacon(url, blob);
            } else {
                fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body }).catch(()=>{});
            }
        } catch(e){}
    };

    const serverConsoleEndpoint = '/api/browserlogs/console';
    const serverNetworkEndpoint = '/api/browserlogs/network';

    // Wrap console methods to stream logs
    ['log','info','warn','error','debug'].forEach((method) => {
        const original = console[method] || console.log;
        console[method] = function(...args) {
            try {
                const payload = { level: method, timestamp: new Date().toISOString(), args: args };
                post(serverConsoleEndpoint, payload);
            } catch(e){}
            original.apply(console, args);
        };
    });

    // Capture global errors
    window.addEventListener('error', function (event) {
        try {
            const payload = {
                level: 'error',
                timestamp: new Date().toISOString(),
                message: event.message,
                filename: event.filename,
                lineno: event.lineno,
                colno: event.colno,
                error: event.error ? (event.error.stack || String(event.error)) : null
            };
            post(serverConsoleEndpoint, payload);
        } catch(e){}
    });

    // Capture unhandled promise rejections
    window.addEventListener('unhandledrejection', function (event) {
        try {
            const payload = {
                level: 'error',
                timestamp: new Date().toISOString(),
                reason: (event.reason && event.reason.stack) ? event.reason.stack : event.reason
            };
            post(serverConsoleEndpoint, payload);
        } catch(e){}
    });

    // Instrument fetch to capture request/response metadata and small portion of response body
    if (window.fetch) {
        const originalFetch = window.fetch.bind(window);
        window.fetch = function(resource, init) {
            const start = Date.now();
            return originalFetch(resource, init).then(response => {
                try {
                    const url = (resource && resource.url) ? resource.url : resource;
                    const clone = response.clone();
                    clone.text().then(bodyText => {
                        const payload = {
                            type: 'fetch',
                            timestamp: new Date().toISOString(),
                            url: url,
                            status: response.status,
                            durationMs: Date.now() - start,
                            responseBodySnippet: bodyText ? bodyText.slice(0, 2000) : null
                        };
                        post(serverNetworkEndpoint, payload);
                    }).catch(()=>{});
                } catch(e){}
                return response;
            }).catch(err => {
                try {
                    const url = (resource && resource.url) ? resource.url : resource;
                    post(serverNetworkEndpoint, { type:'fetch', timestamp: new Date().toISOString(), url: url, error: (err && err.message) ? err.message : String(err) });
                } catch(e){}
                throw err;
            });
        };
    }

    // Instrument XMLHttpRequest
    (function() {
        const OriginalXHR = window.XMLHttpRequest;
        if (!OriginalXHR) return;

        function InstrumentedXHR() {
            const xhr = new OriginalXHR();
            let url = '';
            let start = 0;

            const _open = xhr.open;
            xhr.open = function(method, requestUrl) {
                url = requestUrl;
                return _open.apply(xhr, arguments);
            };

            const _send = xhr.send;
            xhr.send = function() {
                start = Date.now();
                xhr.addEventListener('loadend', function() {
                    try {
                        const payload = {
                            type: 'xhr',
                            timestamp: new Date().toISOString(),
                            url: url,
                            status: xhr.status,
                            durationMs: Date.now() - start,
                            responseSnippet: xhr.responseText ? xhr.responseText.toString().substring(0,2000) : null
                        };
                        post(serverNetworkEndpoint, payload);
                    } catch(e){}
                });
                return _send.apply(xhr, arguments);
            };

            return xhr;
        }

        // Copy prototype to keep instanceof checks working for common libs
        InstrumentedXHR.prototype = OriginalXHR.prototype;
        window.XMLHttpRequest = InstrumentedXHR;
    })();
})();
