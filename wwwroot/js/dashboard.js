// Dashboard page functionality
redirectIfNotAuthenticated();

// Initialize dashboard
document.addEventListener('DOMContentLoaded', async () => {
    await loadUserProfile();
    await loadSubscriptionStatus();
    await loadAnalytics();
    await loadPaymentHistory();
    setupEventListeners();
});

// Navigation
function showSection(sectionId) {
    // Hide all sections
    document.querySelectorAll('.content-section').forEach(section => {
        section.classList.remove('active');
    });

    // Remove active class from nav items
    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.remove('active');
    });

    // Show selected section
    document.getElementById(sectionId).classList.add('active');

    // Add active class to clicked nav item
    event.target.closest('.nav-item').classList.add('active');

    // Update page title
    const titles = {
        'editor': 'Grammar Checker',
        'subscription': 'Manage Subscription',
        'analytics': 'Usage Analytics',
        'profile': 'Account Settings'
    };
    document.getElementById('pageTitle').textContent = titles[sectionId] || 'Dashboard';
}

// Load user profile
async function loadUserProfile() {
    try {
        const response = await authenticatedFetch(`${API_BASE}/auth/profile`);
        if (!response || !response.ok) return;

        const data = await response.json();
        const user = data.user;

        document.getElementById('userEmail').textContent = user.email;
        document.getElementById('profileName').textContent = user.fullName;
        document.getElementById('profileEmail').textContent = user.email;
        document.getElementById('profileCreated').textContent = formatDate(data.createdAt);

        // Update subscription badge
        const badge = document.getElementById('subscriptionBadge');
        badge.textContent = user.subscriptionTier;
        badge.className = `badge ${user.subscriptionTier.toLowerCase()}`;
    } catch (error) {
        console.error('Error loading profile:', error);
    }
}

// Load subscription status
async function loadSubscriptionStatus() {
    try {
        const response = await authenticatedFetch(`${API_BASE}/subscription/current`);
        if (!response || !response.ok) return;

        const data = await response.json();

        // Update current plan info
        const planInfo = document.getElementById('currentPlanInfo');
        const usage = data.currentMonthUsage;
        planInfo.innerHTML = `
            <p><strong>Current Plan:</strong> ${data.currentTier}</p>
            <p><strong>Monthly Corrections:</strong> ${usage.correctionCount} used</p>
            <p><strong>Characters Processed:</strong> ${usage.totalCharactersProcessed}</p>
        `;

        // Update quota bar
        const quotaResponse = await authenticatedFetch(`${API_BASE}/subscription/quota-status`);
        if (quotaResponse && quotaResponse.ok) {
            const quotaData = await quotaResponse.json();
            const fill = document.getElementById('quotaFill');
            fill.style.width = quotaData.percentageUsed + '%';
            document.getElementById('quotaText').textContent = 
                `${quotaData.used} of ${quotaData.limit || 'Unlimited'} - ${quotaData.percentageUsed.toFixed(1)}%`;
        }

        // Update buttons
        const upgradeBtn = document.getElementById('upgradeBtn');
        const downgradeBtn = document.getElementById('downgradeBtn');

        if (data.currentTier === 'Unlimited') {
            upgradeBtn.textContent = 'Current Plan';
            upgradeBtn.disabled = true;
            downgradeBtn.textContent = 'Downgrade to Free';
        } else {
            upgradeBtn.textContent = 'Upgrade Now';
            upgradeBtn.disabled = false;
            downgradeBtn.textContent = 'Current Plan';
            downgradeBtn.disabled = true;
        }
    } catch (error) {
        console.error('Error loading subscription status:', error);
    }
}

// Load analytics
async function loadAnalytics() {
    try {
        const response = await authenticatedFetch(`${API_BASE}/subscription/analytics`);
        if (!response || !response.ok) return;

        const data = await response.json();

        document.getElementById('totalCorrections').textContent = data.totalCorrections;
        document.getElementById('totalCharacters').textContent = data.totalCharactersProcessed.toLocaleString();
        document.getElementById('totalErrors').textContent = data.totalErrorsDetected;
        document.getElementById('averageErrors').textContent = data.averageErrorsPerCorrection.toFixed(2);

        // Display monthly breakdown
        if (data.monthlyBreakdown) {
            const monthlyData = document.getElementById('monthlyData');
            monthlyData.innerHTML = Object.entries(data.monthlyBreakdown)
                .map(([month, count]) => `<p>${month}: ${count} corrections</p>`)
                .join('');
        }
    } catch (error) {
        console.error('Error loading analytics:', error);
    }
}

// Load payment history
async function loadPaymentHistory() {
    try {
        const response = await authenticatedFetch(`${API_BASE}/payment/history`);
        if (!response || !response.ok) return;

        const payments = await response.json();
        const historyList = document.getElementById('paymentHistoryList');

        if (payments.length === 0) {
            historyList.innerHTML = '<p>No payment history</p>';
            return;
        }

        historyList.innerHTML = payments.map(p => `
            <div class="payment-item">
                <div>
                    <div class="payment-item-label">Date</div>
                    <div>${formatDate(p.paymentDate)}</div>
                </div>
                <div>
                    <div class="payment-item-label">Amount</div>
                    <div>${p.amountFormatted}</div>
                </div>
                <div>
                    <div class="payment-item-label">Plan</div>
                    <div>${p.subscriptionTier}</div>
                </div>
                <div>
                    <div class="payment-item-label">Status</div>
                    <div><span class="badge ${p.status.toLowerCase()}">${p.status}</span></div>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Error loading payment history:', error);
    }
}

// Grammar check
async function checkGrammar() {
    const text = document.getElementById('textInput').value;
    const language = document.getElementById('languageSelect').value;

    if (!text.trim()) {
        showNotification('Please enter some text', 'error');
        return;
    }

    try {
        const response = await authenticatedFetch(`${API_BASE}/grammar/check`, {
            method: 'POST',
            body: JSON.stringify({ text, language })
        });

        if (!response) return;

        if (response.status === 429) {
            showNotification('You have exceeded your monthly quota. Upgrade to continue.', 'error');
            return;
        }

        if (!response.ok) {
            const data = await response.json();
            showNotification(data.error || 'Error checking grammar', 'error');
            return;
        }

        const result = await response.json();
        displayCorrections(result);
        loadSubscriptionStatus(); // Refresh quota
    } catch (error) {
        console.error('Error checking grammar:', error);
        showNotification('An error occurred', 'error');
    }
}

// Display corrections
function displayCorrections(result) {
    const correctionsList = document.getElementById('correctionsList');
    const correctedText = document.getElementById('correctedText');
    const correctedTextContent = document.getElementById('correctedTextContent');

    if (!result.matches || result.matches.length === 0) {
        correctionsList.innerHTML = '<p class="placeholder">No issues found! ✓</p>';
        correctedText.style.display = 'none';
        return;
    }

    correctionsList.innerHTML = result.matches.map(match => `
        <div class="correction-item ${match.issueType || 'info'}">
            <div class="correction-title">${match.shortMessage || 'Issue'}</div>
            <div class="correction-message">
                <strong>"${match.originalText}"</strong> → 
                ${match.suggestions?.length > 0 ? match.suggestions.map(s => `<strong>"${s}"</strong>`).join(', ') : 'No suggestions'}
            </div>
            <small>${match.message}</small>
        </div>
    `).join('');

    correctedTextContent.textContent = result.correctedText || result.originalText;
    correctedText.style.display = 'block';
}

// Initiate upgrade
async function initiateUpgrade() {
    try {
        const response = await authenticatedFetch(`${API_BASE}/subscription/upgrade`, {
            method: 'POST'
        });

        if (!response || !response.ok) {
            showNotification('Failed to initiate upgrade', 'error');
            return;
        }

        const data = await response.json();
        openPaymentModal(data);
    } catch (error) {
        console.error('Error initiating upgrade:', error);
        showNotification('An error occurred', 'error');
    }
}

// Downgrade subscription
async function downgradeSubscription() {
    if (!confirm('Are you sure you want to downgrade to the Free tier?')) {
        return;
    }

    try {
        const response = await authenticatedFetch(`${API_BASE}/subscription/downgrade`, {
            method: 'POST'
        });

        if (!response || !response.ok) {
            showNotification('Failed to downgrade subscription', 'error');
            return;
        }

        showNotification('Subscription downgraded to Free tier', 'success');
        await loadSubscriptionStatus();
    } catch (error) {
        console.error('Error downgrading subscription:', error);
        showNotification('An error occurred', 'error');
    }
}

// Change password
async function changePassword() {
    const current = document.getElementById('currentPassword').value;
    const newPwd = document.getElementById('newPassword').value;
    const confirm = document.getElementById('confirmPassword').value;
    const errorDiv = document.getElementById('passwordError');

    errorDiv.textContent = '';

    if (!current || !newPwd || !confirm) {
        errorDiv.textContent = 'All fields are required';
        return;
    }

    if (newPwd !== confirm) {
        errorDiv.textContent = 'Passwords do not match';
        return;
    }

    if (newPwd.length < 8) {
        errorDiv.textContent = 'New password must be at least 8 characters';
        return;
    }

    try {
        const response = await authenticatedFetch(`${API_BASE}/auth/change-password`, {
            method: 'POST',
            body: JSON.stringify({
                currentPassword: current,
                newPassword: newPwd
            })
        });

        if (!response || !response.ok) {
            const data = await response?.json();
            errorDiv.textContent = data?.error || 'Failed to change password';
            return;
        }

        showNotification('Password changed successfully', 'success');
        document.getElementById('currentPassword').value = '';
        document.getElementById('newPassword').value = '';
        document.getElementById('confirmPassword').value = '';
    } catch (error) {
        console.error('Error changing password:', error);
        errorDiv.textContent = 'An error occurred';
    }
}

// Payment modal handling
function openPaymentModal(paymentData) {
    // Initialize Stripe (you'll need to set the publishable key from appsettings)
    const modal = document.getElementById('paymentModal');
    modal.style.display = 'flex';
    // Payment will be handled by Stripe Elements
}

function closePaymentModal() {
    document.getElementById('paymentModal').style.display = 'none';
}

async function completePayment() {
    // This would integrate with Stripe Elements for payment processing
    showNotification('Payment processing...', 'info');
}

// Setup event listeners
function setupEventListeners() {
    // Auto-update character count
    document.getElementById('textInput').addEventListener('input', (e) => {
        const count = e.target.value.length;
        document.getElementById('charCount').textContent = `${count}/5000 characters`;
    });

    // Close modal on outside click
    window.addEventListener('click', (e) => {
        const modal = document.getElementById('paymentModal');
        if (e.target === modal) {
            closePaymentModal();
        }
    });
}
