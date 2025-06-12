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