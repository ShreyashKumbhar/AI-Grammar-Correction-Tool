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
function showSection(sectionId, evt) {
    document.querySelectorAll('.content-section').forEach(section => {
        section.classList.remove('active');
    });

    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.remove('active');
    });

    document.getElementById(sectionId).classList.add('active');

    const navItem = evt?.target?.closest('.nav-item');
    if (navItem) navItem.classList.add('active');

    const titles = {
        profile: 'Account Settings',
        activity: 'Activity',
        subscription: 'Manage Subscription'
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
        pendingPaymentOrder = data;
        await startRazorpayCheckout();
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

let pendingPaymentOrder = null;

function openPaymentModal(paymentData) {
    pendingPaymentOrder = paymentData;
    const errorEl = document.getElementById('payment-errors');
    if (errorEl) errorEl.textContent = '';
    document.getElementById('paymentModal').style.display = 'flex';
}

function closePaymentModal() {
    document.getElementById('paymentModal').style.display = 'none';
    pendingPaymentOrder = null;
}

async function startRazorpayCheckout() {
    if (!pendingPaymentOrder?.orderId || !pendingPaymentOrder?.keyId) {
        showNotification('Payment session expired. Please try again.', 'error');
        return;
    }

    if (typeof Razorpay === 'undefined') {
        showNotification('Payment gateway failed to load. Refresh and try again.', 'error');
        return;
    }

    const user = getCurrentUser();
    const options = {
        key: pendingPaymentOrder.keyId,
        amount: pendingPaymentOrder.amount,
        currency: pendingPaymentOrder.currency || 'INR',
        name: 'Prose',
        description: 'Unlimited subscription (1 month)',
        order_id: pendingPaymentOrder.orderId,
        prefill: {
            name: user?.fullName || '',
            email: user?.email || ''
        },
        theme: { color: '#c8a96e' },
        handler: async function (response) {
            await verifyRazorpayPayment(response);
        },
        modal: {
            ondismiss: function () {
                showNotification('Payment cancelled.', 'info');
            }
        }
    };

    const rzp = new Razorpay(options);
    rzp.on('payment.failed', function (resp) {
        const msg = resp.error?.description || 'Payment failed.';
        const errorEl = document.getElementById('payment-errors');
        if (errorEl) errorEl.textContent = msg;
        showNotification(msg, 'error');
    });
    rzp.open();
    closePaymentModal();
}

async function verifyRazorpayPayment(response) {
    try {
        const res = await authenticatedFetch(`${API_BASE}/payment/verify`, {
            method: 'POST',
            body: JSON.stringify({
                razorpayOrderId: response.razorpay_order_id,
                razorpayPaymentId: response.razorpay_payment_id,
                razorpaySignature: response.razorpay_signature
            })
        });

        if (!res?.ok) {
            const data = await res?.json();
            showNotification(data?.error || 'Payment verification failed.', 'error');
            return;
        }

        showNotification('Upgrade successful! Welcome to Unlimited.', 'success');
        await loadUserProfile();
        await loadSubscriptionStatus();
        await loadPaymentHistory();
    } catch (error) {
        console.error('Payment verification error:', error);
        showNotification('Could not verify payment. Contact support if you were charged.', 'error');
    }
}

// Setup event listeners
function setupEventListeners() {
    window.addEventListener('click', (e) => {
        const modal = document.getElementById('paymentModal');
        if (e.target === modal) {
            closePaymentModal();
        }
    });
}
