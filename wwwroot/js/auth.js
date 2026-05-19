// Authentication utilities and helpers
const API_BASE = '/api';

// Get authorization header
function getAuthHeader() {
    const token = localStorage.getItem('token');
    return {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    };
}

// Check if user is authenticated
function isAuthenticated() {
    return !!localStorage.getItem('token');
}

// Get current user from localStorage
function getCurrentUser() {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
}

// Logout
function logout(skipConfirm = false) {
    if (!skipConfirm && !confirm('Are you sure you want to logout?')) return;

    localStorage.removeItem('token');
    localStorage.removeItem('user');

    const onAuthPage = window.location.pathname.endsWith('auth.html');
    window.location.href = onAuthPage ? '/auth.html' : '/';
}

// Redirect to auth page if not authenticated
function redirectIfNotAuthenticated() {
    if (!isAuthenticated()) {
        window.location.href = '/auth.html';
    }
}

// Fetch with authentication
async function authenticatedFetch(url, options = {}) {
    const response = await fetch(url, {
        ...options,
        headers: {
            ...getAuthHeader(),
            ...options.headers
        }
    });

    // Handle 401 Unauthorized
    if (response.status === 401) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        window.location.href = '/auth.html';
        return null;
    }

    return response;
}

// Show notification
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        padding: 15px 20px;
        border-radius: 8px;
        background: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#6366f1'};
        color: white;
        z-index: 2000;
        animation: slideIn 0.3s ease;
    `;
    document.body.appendChild(notification);
    setTimeout(() => notification.remove(), 3000);
}

// Format currency
function formatCurrency(paise) {
    return `₹${(paise / 100).toLocaleString('en-IN')}`;
}

// Format date
function formatDate(date) {
    return new Date(date).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

// Debounce function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}
