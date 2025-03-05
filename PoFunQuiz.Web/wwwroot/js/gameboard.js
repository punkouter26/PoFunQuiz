// GameBoard touch and mobile enhancements

// Function to focus the game container
window.focusGameContainer = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.focus();
    }
};

// Function to detect if the device is a mobile device
window.isMobileDevice = function () {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
        (window.innerWidth <= 768);
};

// Function to add touch feedback to answer options
window.addTouchFeedback = function () {
    try {
        const answerOptions = document.querySelectorAll('.answer-option');
        
        if (!answerOptions || answerOptions.length === 0) {
            console.log("No answer options found for touch feedback");
            return;
        }
        
        console.log(`Adding touch feedback to ${answerOptions.length} answer options`);
        
        answerOptions.forEach(option => {
            // Remove existing listeners to prevent duplicates
            option.removeEventListener('touchstart', handleTouchStart);
            option.removeEventListener('touchend', handleTouchEnd);
            
            // Add new listeners
            option.addEventListener('touchstart', handleTouchStart);
            option.addEventListener('touchend', handleTouchEnd);
        });
        
        console.log("Touch feedback added successfully");
        return true;
    } catch (error) {
        console.error("Error adding touch feedback:", error);
        return false;
    }
};

// Touch event handlers
function handleTouchStart(e) {
    this.classList.add('touch-active');
}

function handleTouchEnd(e) {
    this.classList.remove('touch-active');
}

// Function to add touch swipe detection
window.addTouchSupport = function (elementId, dotNetReference) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.warn(`Element with ID ${elementId} not found for touch support`);
        return false;
    }
    
    if (!dotNetReference) {
        console.warn("No .NET reference provided for touch support");
        return false;
    }
    
    let touchStartX = 0;
    let touchStartY = 0;
    
    element.addEventListener('touchstart', function (e) {
        touchStartX = e.changedTouches[0].screenX;
        touchStartY = e.changedTouches[0].screenY;
    }, false);
    
    element.addEventListener('touchend', function (e) {
        const touchEndX = e.changedTouches[0].screenX;
        const touchEndY = e.changedTouches[0].screenY;
        
        const diffX = touchEndX - touchStartX;
        const diffY = touchEndY - touchStartY;
        
        // Detect horizontal swipe (for navigation between questions)
        if (Math.abs(diffX) > Math.abs(diffY) && Math.abs(diffX) > 50) {
            try {
                if (diffX > 0) {
                    // Swipe right - previous question
                    dotNetReference.invokeMethodAsync('HandleSwipe', 'right');
                } else {
                    // Swipe left - next question
                    dotNetReference.invokeMethodAsync('HandleSwipe', 'left');
                }
            } catch (error) {
                console.error("Error invoking .NET method:", error);
            }
        }
    }, false);
    
    // Prevent zoom on double tap
    element.addEventListener('dblclick', function (e) {
        e.preventDefault();
    });
    
    console.log("Touch support added to game container");
    return true;
};

// Function to optimize for mobile
window.optimizeForMobile = function () {
    if (window.isMobileDevice()) {
        // Add viewport meta tag to prevent zooming
        const viewportMeta = document.querySelector('meta[name="viewport"]');
        if (viewportMeta) {
            viewportMeta.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';
        }
        
        // Add class to body for mobile-specific styling
        document.body.classList.add('mobile-device');
        
        console.log("Mobile optimizations applied");
        return true;
    }
    return false;
};

// Initialize mobile optimizations when the page loads
document.addEventListener('DOMContentLoaded', function () {
    window.optimizeForMobile();
}); 