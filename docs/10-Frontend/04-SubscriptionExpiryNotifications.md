# Subscription Expiry Notifications Guide

> **Automated Email Reminders for Subscription Lifecycle**  
> Base URL: `https://api.example.com/api/v1`

---

## Overview

The system automatically sends email reminders at key subscription lifecycle events:

| Event | When | Email Sent |
|-------|------|-----------|
| Subscription activated | Immediately | Welcome email |
| 7 days before expiry | Auto-check daily | "Renews in 7 days" |
| 3 days before expiry | Auto-check daily | "Renews in 3 days" |
| 1 day before expiry | Auto-check daily | "Renews tomorrow" |
| Day of expiry | Auto-check daily | "Your subscription expires today" |
| After expiry | Auto-check daily | "Your subscription has expired" |

---

## How It Works

### Background Job

A background service (`SubscriptionExpiryNotificationBackgroundService`) runs every hour:

```
Every hour at :00
  ↓
Query users with active subscriptions
  ↓
For each subscription:
  - Calculate days until expiry
  - Check if reminder already sent (prevent duplicates)
  - If matches 7/3/1 day or expiry → Send email
  ↓
Log results
```

### Architecture

```
Database (UserSubscription)
    ↓
Background Service (runs hourly)
    ↓
Email Template Engine
    ↓
Brevo API
    ↓
User's email inbox
```

---

## Email Templates

### 1. Subscription Reminder - 7 Days

**Template Name**: `subscription_reminder_7days`

**When**: 7 days before expiry

**Subject**: Your KromicFlow subscription renews in 7 days

**Body Template**:
```html
<h2>Your subscription renews in 7 days</h2>
<p>Hi {{userName}},</p>
<p>Your {{planName}} plan will renew on <strong>{{renewalDate}}</strong>.</p>
<p>Current features:</p>
<ul>
  <li>{{maxAccounts}} connected accounts</li>
  <li>{{maxAutomations}} automations</li>
  <li>{{monthlyRuns}} monthly automation runs</li>
</ul>
<p>
  <a href="{{dashboardUrl}}/billing">Manage Subscription</a>
</p>
```

### 2. Subscription Reminder - 3 Days

**Template Name**: `subscription_reminder_3days`

**When**: 3 days before expiry

**Subject**: Your KromicFlow subscription renews in 3 days

**Body**: Similar to 7 days, emphasizing "3 days"

### 3. Subscription Reminder - 1 Day

**Template Name**: `subscription_reminder_1day`

**When**: 1 day before expiry

**Subject**: Your KromicFlow subscription renews tomorrow

**Body**: Similar to 7 days, emphasizing "tomorrow"

### 4. Subscription Expired

**Template Name**: `subscription_expired`

**When**: On expiry date

**Subject**: Your KromicFlow subscription has expired

**Body Template**:
```html
<h2>Your subscription has expired</h2>
<p>Hi {{userName}},</p>
<p>Your {{planName}} plan expired on <strong>{{expiryDate}}</strong>.</p>
<p>Your account has been downgraded to the Free plan with limited features:</p>
<ul>
  <li>1 connected account</li>
  <li>3 automations</li>
  <li>500 monthly automation runs</li>
</ul>
<p>
  <a href="{{dashboardUrl}}/billing/plans">Upgrade Your Plan</a>
</p>
```

### 5. Subscription Charged (Successful Renewal)

**Template Name**: `subscription_charged`

**When**: Razorpay webhook fired

**Subject**: Your KromicFlow subscription renewed successfully

**Body Template**:
```html
<h2>Subscription renewed</h2>
<p>Hi {{userName}},</p>
<p>Your {{planName}} plan has been successfully renewed.</p>
<p>Subscription details:</p>
<ul>
  <li>Plan: {{planName}}</li>
  <li>Amount: {{amount}} {{currency}}</li>
  <li>Next renewal: {{nextRenewalDate}}</li>
</ul>
<p>
  <a href="{{dashboardUrl}}/billing">View Invoice</a>
</p>
```

### 6. Subscription Halted (Payment Failed)

**Template Name**: `subscription_failed`

**When**: Razorpay webhook fired

**Subject**: Action needed: Your KromicFlow subscription payment failed

**Body Template**:
```html
<h2>Payment failed</h2>
<p>Hi {{userName}},</p>
<p>Your subscription payment failed on <strong>{{failureDate}}</strong>.</p>
<p>Your {{planName}} plan features are now disabled.</p>
<p>Please update your payment method:</p>
<ul>
  <li>Retry amount: {{amount}} {{currency}}</li>
  <li>Reason: {{failureReason}}</li>
</ul>
<p>
  <a href="{{dashboardUrl}}/billing">Update Payment Method</a>
</p>
```

---

## Frontend Integration

### 1. Subscription Status Display

```jsx
import React from 'react';

export function SubscriptionCard({ subscription, plan }) {
  const daysUntilExpiry = calculateDaysUntilExpiry(subscription.currentPeriodEnd);
  
  const getStatusColor = () => {
    if (daysUntilExpiry < 0) return 'bg-red-50 border-red-200';
    if (daysUntilExpiry <= 7) return 'bg-yellow-50 border-yellow-200';
    return 'bg-green-50 border-green-200';
  };

  const getStatusMessage = () => {
    if (daysUntilExpiry < 0) {
      return 'Expired - Downgraded to Free Plan';
    }
    if (daysUntilExpiry === 0) {
      return 'Expires today';
    }
    return `Renews in ${daysUntilExpiry} days`;
  };

  return (
    <div className={`border rounded-lg p-4 ${getStatusColor()}`}>
      <h3 className="font-semibold">{plan.name}</h3>
      <p className="text-sm text-gray-600 mt-2">
        {getStatusMessage()}
      </p>
      <p className="text-xs text-gray-500 mt-1">
        Renewal date: {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
      </p>
      
      {daysUntilExpiry <= 7 && daysUntilExpiry > 0 && (
        <button className="mt-3 px-3 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
          Manage Subscription
        </button>
      )}
    </div>
  );
}

function calculateDaysUntilExpiry(expiryDate) {
  const now = new Date();
  const expiry = new Date(expiryDate);
  const diffTime = expiry - now;
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}
```

### 2. Dashboard Widget

```jsx
export function SubscriptionExpiryWidget() {
  const { subscription, plan } = useSubscription();
  
  if (!subscription) return null;

  const daysLeft = calculateDaysUntilExpiry(subscription.currentPeriodEnd);

  if (daysLeft < 0) {
    return (
      <div className="bg-red-50 border-l-4 border-red-500 p-4">
        <p className="font-semibold text-red-800">Subscription Expired</p>
        <p className="text-sm text-red-700 mt-1">
          Your plan has expired. Upgrade now to restore features.
        </p>
        <a href="/billing/plans" className="mt-2 inline-block px-4 py-2 bg-red-600 text-white rounded">
          Upgrade Plan
        </a>
      </div>
    );
  }

  if (daysLeft <= 7) {
    return (
      <div className="bg-yellow-50 border-l-4 border-yellow-500 p-4">
        <p className="font-semibold text-yellow-800">Subscription Renewing Soon</p>
        <p className="text-sm text-yellow-700 mt-1">
          Your {plan.name} plan renews in {daysLeft} {daysLeft === 1 ? 'day' : 'days'}.
        </p>
        <a href="/billing" className="mt-2 inline-block px-4 py-2 bg-yellow-600 text-white rounded">
          Manage Subscription
        </a>
      </div>
    );
  }

  return (
    <div className="bg-green-50 border-l-4 border-green-500 p-4">
      <p className="text-sm text-green-700">
        ✓ Your {plan.name} plan is active until {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
      </p>
    </div>
  );
}
```

### 3. Billing Page Status

```jsx
export function BillingPage() {
  const { subscription, plan } = useSubscription();

  return (
    <div className="max-w-2xl mx-auto">
      <h1 className="text-3xl font-bold mb-8">Billing & Subscription</h1>

      {/* Current Plan */}
      <div className="bg-white p-6 rounded-lg shadow mb-8">
        <h2 className="text-xl font-semibold mb-4">Current Plan</h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <p className="text-gray-600 text-sm">Plan</p>
            <p className="font-semibold text-lg">{plan.name}</p>
          </div>
          <div>
            <p className="text-gray-600 text-sm">Status</p>
            <p className="font-semibold text-lg">
              {subscription.status === 'active' ? '🟢 Active' : '🔴 Inactive'}
            </p>
          </div>
          <div>
            <p className="text-gray-600 text-sm">Next Renewal</p>
            <p className="font-semibold text-lg">
              {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
            </p>
          </div>
          <div>
            <p className="text-gray-600 text-sm">Price</p>
            <p className="font-semibold text-lg">₹{(plan.priceInrPaise / 100).toFixed(2)}/month</p>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="flex gap-4">
        <button className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
          Change Plan
        </button>
        <button className="px-4 py-2 border border-red-600 text-red-600 rounded hover:bg-red-50">
          Cancel Subscription
        </button>
      </div>
    </div>
  );
}
```

---

## Variable Placeholders

Available in all email templates:

| Placeholder | Description | Example |
|------------|-------------|---------|
| `{{userName}}` | User's full name | John Doe |
| `{{planName}}` | Plan name | Starter |
| `{{renewalDate}}` | Next renewal date | July 25, 2026 |
| `{{expiryDate}}` | Expiry date | July 20, 2026 |
| `{{daysUntilExpiry}}` | Days remaining | 7 |
| `{{maxAccounts}}` | Max Instagram accounts | 2 |
| `{{maxAutomations}}` | Max automations allowed | 10 |
| `{{monthlyRuns}}` | Monthly execution limit | 2000 |
| `{{amount}}` | Charge amount | 9900 |
| `{{currency}}` | Currency code | INR |
| `{{dashboardUrl}}` | Dashboard base URL | https://yourdomain.com/dashboard |
| `{{failureReason}}` | Payment failure reason | Card declined |
| `{{failureDate}}` | When payment failed | July 20, 2026 10:30 AM |

---

## Subscription Lifecycle States

### State Diagram

```
┌──────────┐
│ created  │  User initiates subscription
└─────┬────┘
      │
      ↓
┌──────────────────┐
│  authenticated   │  Payment verified by Razorpay
└─────┬────────────┘
      │
      ↓
┌──────────────────┐
│     active       │  Subscription is valid
└──┬───────────┬───┘
   │           │
   │ (renewed) │ (payment failed)
   │           │
   ↓           ↓
 active    ┌────────┐
           │ halted │  Payment failed, waiting for retry
           └────┬───┘
                │
                ├─→ active (retry successful)
                │
                └─→ cancelled (user cancels)
                
┌──────────────────┐
│   cancelled      │  Subscription ended by user
└──────────────────┘

┌──────────────────┐
│    completed     │  Subscription contract ended (auto-cancel after X cycles)
└──────────────────┘

┌──────────────────┐
│    expired       │  Subscription period ended (downgrade to free plan)
└──────────────────┘
```

### Email Timeline

```
Day 1: User subscribes
  ├─ Email: "Welcome to [Plan] plan"
  └─ Status: active

Day 24: 7 days before expiry (monthly billing)
  ├─ Email: "Your subscription renews in 7 days"
  └─ Status: active (sending reminder)

Day 27: 3 days before expiry
  ├─ Email: "Your subscription renews in 3 days"
  └─ Status: active (sending reminder)

Day 30: 1 day before expiry
  ├─ Email: "Your subscription renews tomorrow"
  └─ Status: active (sending reminder)

Day 31: Expiry day
  ├─ Razorpay charges: SUCCESS
  ├─ Email: "Subscription renewed successfully"
  ├─ Status: active (renewed)
  └─ Next expiry: Day 62
  
  OR
  
  ├─ Razorpay charges: FAILED
  ├─ Email: "Payment failed, features disabled"
  ├─ Status: halted
  └─ User must update payment method
```

---

## Backend API for Frontend Reference

### GET /api/v1/billing/status

Returns subscription status including renewal date

```json
{
  "plan": "STARTER",
  "status": "active",
  "currentPeriodStart": "2026-06-20T00:00:00Z",
  "currentPeriodEnd": "2026-07-20T00:00:00Z",
  "cancelledAtUtc": null,
  "razorpaySubscriptionId": "sub_123456"
}
```

**Calculate days until expiry**:
```javascript
const expiryDate = new Date(subscription.currentPeriodEnd);
const today = new Date();
const daysLeft = Math.ceil((expiryDate - today) / (1000 * 60 * 60 * 24));
```

### GET /api/v1/billing/plans

Get plan details (includes feature limits)

```json
{
  "id": "...",
  "code": "STARTER",
  "name": "Starter",
  "priceInrPaise": 9900,
  "features": {
    "maxAccounts": 2,
    "maxAutomations": 10,
    "monthlyRuns": 2000
  }
}
```

---

## Testing Subscription Reminders

### Manual Testing

1. **Create test subscription**:
   - Subscribe to any plan
   - Verify subscription status in DB

2. **Simulate time progression**:
   - Manually update `current_period_end` to tomorrow
   - Run background service
   - Check if reminder email sent

3. **Verify email content**:
   - Check Brevo email logs
   - Verify template variables filled
   - Check links work

### Automated Tests

```javascript
describe('SubscriptionExpiryNotifications', () => {
  it('should send email 7 days before expiry', async () => {
    const expiry = addDays(today, 7);
    const shouldSend = await service.shouldSendReminder(subscription, expiry);
    expect(shouldSend).toBe(true);
  });

  it('should not send duplicate reminders', async () => {
    // Simulate email already sent 7 days ago
    subscription.lastReminderSentAt = addDays(today, -7);
    
    const shouldSend = await service.shouldSendReminder(subscription, expiry);
    expect(shouldSend).toBe(false);
  });

  it('should send expiry email on expiry day', async () => {
    const expiry = today;
    const shouldSend = await service.shouldSendReminder(subscription, expiry);
    expect(shouldSend).toBe(true);
  });
});
```

---

## Frontend Checklist

- [ ] Display subscription status on dashboard
- [ ] Show warning if expiring soon (<7 days)
- [ ] Show error if expired
- [ ] Link to billing page
- [ ] Handle status widget on all relevant pages
- [ ] Test with real subscription
- [ ] Verify emails received in test inbox
- [ ] Check email links work
- [ ] Confirm variables filled correctly
- [ ] Test renewal flow
- [ ] Test failed payment handling

---

## Summary

**Automatic emails sent at**:
- ✅ 7 days before expiry
- ✅ 3 days before expiry
- ✅ 1 day before expiry
- ✅ On expiry date
- ✅ On successful renewal
- ✅ On payment failure

**Frontend shows**:
- ✅ Current plan and renewal date
- ✅ Warning if expiring soon
- ✅ Error if expired
- ✅ Links to manage subscription

**No frontend action needed** - reminders are automatic!

---

**Last Updated**: July 20, 2026  
**Status**: Ready for deployment
