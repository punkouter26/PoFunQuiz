// Mobile UX Utilities - Haptic Feedback & Interactions
window.mobileUX = window.mobileUX || {};

/**
 * Haptic Feedback API
 * Provides tactile feedback for mobile interactions
 */
window.mobileUX.haptic = {
    /**
     * Short vibration pulse (correct answer)
     */
    success: function() {
        if ('vibrate' in navigator) {
            navigator.vibrate(50); // 50ms pulse
        }
    },

    /**
     * Double pulse (incorrect answer)
     */
    error: function() {
        if ('vibrate' in navigator) {
            navigator.vibrate([100, 50, 100]); // vibrate-pause-vibrate
        }
    },

    /**
     * Single tap confirmation
     */
    tap: function() {
        if ('vibrate' in navigator) {
            navigator.vibrate(10); // Very short pulse
        }
    },

    /**
     * Medium pulse for important actions
     */
    notification: function() {
        if ('vibrate' in navigator) {
            navigator.vibrate(200);
        }
    },

    /**
     * Custom vibration pattern
     * @param {number|number[]} pattern - Duration in ms or pattern array
     */
    custom: function(pattern) {
        if ('vibrate' in navigator) {
            navigator.vibrate(pattern);
        }
    },

    /**
     * Check if haptic feedback is available
     * @returns {boolean}
     */
    isAvailable: function() {
        return 'vibrate' in navigator;
    }
};

/**
 * Swipe Gesture Detection
 */
window.mobileUX.swipe = {
    _touchStartX: 0,
    _touchStartY: 0,
    _touchEndX: 0,
    _touchEndY: 0,
    _minSwipeDistance: 50,

    /**
     * Initialize swipe detection on an element
     * @param {HTMLElement} element - Element to track swipes on
     * @param {Object} callbacks - { onSwipeLeft, onSwipeRight, onSwipeUp, onSwipeDown }
     */
    init: function(element, callbacks) {
        if (!element) return;

        element.addEventListener('touchstart', (e) => {
            this._touchStartX = e.changedTouches[0].screenX;
            this._touchStartY = e.changedTouches[0].screenY;
        }, { passive: true });

        element.addEventListener('touchend', (e) => {
            this._touchEndX = e.changedTouches[0].screenX;
            this._touchEndY = e.changedTouches[0].screenY;
            this._handleSwipe(callbacks);
        }, { passive: true });
    },

    _handleSwipe: function(callbacks) {
        const diffX = this._touchEndX - this._touchStartX;
        const diffY = this._touchEndY - this._touchStartY;
        const absX = Math.abs(diffX);
        const absY = Math.abs(diffY);

        // Horizontal swipe
        if (absX > absY && absX > this._minSwipeDistance) {
            if (diffX > 0 && callbacks.onSwipeRight) {
                callbacks.onSwipeRight();
            } else if (diffX < 0 && callbacks.onSwipeLeft) {
                callbacks.onSwipeLeft();
            }
        }
        // Vertical swipe
        else if (absY > this._minSwipeDistance) {
            if (diffY > 0 && callbacks.onSwipeDown) {
                callbacks.onSwipeDown();
            } else if (diffY < 0 && callbacks.onSwipeUp) {
                callbacks.onSwipeUp();
            }
        }
    }
};

/**
 * Skeleton Loader Management
 */
window.mobileUX.skeleton = {
    /**
     * Show skeleton loader
     * @param {string} containerId - Container element ID
     * @param {string} type - Skeleton type ('question', 'card', 'list')
     */
    show: function(containerId, type = 'question') {
        const container = document.getElementById(containerId);
        if (!container) return;

        let skeletonHTML = '';
        
        switch(type) {
            case 'question':
                skeletonHTML = `
                    <div class="question-skeleton">
                        <div class="skeleton skeleton-title"></div>
                        <div class="skeleton skeleton-button"></div>
                        <div class="skeleton skeleton-button"></div>
                        <div class="skeleton skeleton-button"></div>
                        <div class="skeleton skeleton-button"></div>
                    </div>
                `;
                break;
            case 'card':
                skeletonHTML = `
                    <div class="skeleton skeleton-card"></div>
                    <div class="skeleton skeleton-card"></div>
                `;
                break;
            case 'list':
                skeletonHTML = `
                    <div class="skeleton skeleton-text" style="width: 90%"></div>
                    <div class="skeleton skeleton-text" style="width: 85%"></div>
                    <div class="skeleton skeleton-text" style="width: 95%"></div>
                    <div class="skeleton skeleton-text" style="width: 80%"></div>
                `;
                break;
        }

        container.innerHTML = skeletonHTML;
    },

    /**
     * Hide skeleton loader
     * @param {string} containerId - Container element ID
     */
    hide: function(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = '';
    }
};

/**
 * Bottom Sheet / Expandable Sections
 */
window.mobileUX.bottomSheet = {
    /**
     * Toggle expandable section
     * @param {HTMLElement} element - Section element
     */
    toggle: function(element) {
        if (!element) return;
        
        element.classList.toggle('expanded');
        this._hapticFeedback();
    },

    /**
     * Expand section
     * @param {HTMLElement} element - Section element
     */
    expand: function(element) {
        if (!element) return;
        element.classList.add('expanded');
    },

    /**
     * Collapse section
     * @param {HTMLElement} element - Section element
     */
    collapse: function(element) {
        if (!element) return;
        element.classList.remove('expanded');
    },

    /**
     * Expand all sections
     * @param {string} containerSelector - Container CSS selector
     */
    expandAll: function(containerSelector) {
        document.querySelectorAll(containerSelector + ' .results-section').forEach(section => {
            section.classList.add('expanded');
        });
    },

    /**
     * Collapse all sections
     * @param {string} containerSelector - Container CSS selector
     */
    collapseAll: function(containerSelector) {
        document.querySelectorAll(containerSelector + ' .results-section').forEach(section => {
            section.classList.remove('expanded');
        });
    },

    _hapticFeedback: function() {
        if (window.mobileUX.haptic) {
            window.mobileUX.haptic.tap();
        }
    }
};

/**
 * Auto-hide navigation bar on scroll
 */
window.mobileUX.scrollNav = {
    _lastScrollTop: 0,
    _scrollThreshold: 50,

    /**
     * Initialize scroll-based navigation hiding
     * @param {string} navSelector - Navigation element selector
     */
    init: function(navSelector = '.app-bar') {
        const nav = document.querySelector(navSelector);
        if (!nav) return;

        let ticking = false;

        window.addEventListener('scroll', () => {
            if (!ticking) {
                window.requestAnimationFrame(() => {
                    this._handleScroll(nav);
                    ticking = false;
                });
                ticking = true;
            }
        }, { passive: true });
    },

    _handleScroll: function(nav) {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

        if (Math.abs(scrollTop - this._lastScrollTop) < this._scrollThreshold) {
            return;
        }

        if (scrollTop > this._lastScrollTop && scrollTop > 100) {
            // Scrolling down - hide nav
            nav.classList.add('hidden');
        } else {
            // Scrolling up - show nav
            nav.classList.remove('hidden');
        }

        this._lastScrollTop = scrollTop;
    }
};

/**
 * Add visual feedback class to element
 * @param {HTMLElement} element - Element to animate
 * @param {string} feedbackClass - CSS class to add ('haptic-pulse', 'answer-correct', 'answer-incorrect')
 * @param {number} duration - Duration in ms before removing class
 */
window.mobileUX.visualFeedback = function(element, feedbackClass, duration = 500) {
    if (!element) return;
    
    element.classList.add(feedbackClass);
    
    setTimeout(() => {
        element.classList.remove(feedbackClass);
    }, duration);
};

/**
 * Device detection utilities
 */
window.mobileUX.device = {
    isMobile: function() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    },
    
    isIOS: function() {
        return /iPhone|iPad|iPod/i.test(navigator.userAgent);
    },
    
    isAndroid: function() {
        return /Android/i.test(navigator.userAgent);
    },
    
    hasTouch: function() {
        return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
    },
    
    getViewportWidth: function() {
        return Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0);
    },
    
    getViewportHeight: function() {
        return Math.max(document.documentElement.clientHeight || 0, window.innerHeight || 0);
    },
    
    isPortrait: function() {
        return window.innerHeight > window.innerWidth;
    }
};

// Initialize auto-hide navigation on page load
if (window.mobileUX.device.isMobile()) {
    document.addEventListener('DOMContentLoaded', () => {
        window.mobileUX.scrollNav.init();
    });
}

console.log('✅ Mobile UX utilities loaded');
