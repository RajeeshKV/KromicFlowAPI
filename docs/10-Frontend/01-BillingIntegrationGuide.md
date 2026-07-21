# KromicFlow Billing — Frontend Integration Guide

> Razorpay Subscriptions — Production-Grade Implementation  
> Base URL: `https://flowapi.kromic.in/api/v1`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Environment Setup](#2-environment-setup)
3. [Plans API](#3-plans-api)
4. [Subscription Checkout Flow](#4-subscription-checkout-flow)
5. [Verify Payment](#5-verify-payment)
6. [Subscription Status](#6-subscription-status)
7. [Cancel Subscription](#7-cancel-subscription)
8. [Razorpay Webhook Events](#8-razorpay-webhook-events)
9. [UI Screen Reference](#9-ui-screen-reference)
10. [Error States](#10-error-states)
11. [Test Mode vs Live Mode](#11-test-mode-vs-live-mode)
12. [Security Rules](#12-security-rules)

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Frontend                                                        │
│                                                                  │
│  1. GET /billing/plans        → show plan selection UI           │
│  2. POST /billing/subscribe   → get subscription_id + key_id    │
│  3. Razorpay Checkout opens   → user pays                       │
│  4. POST /billing/verify      → verify signature server-side    │
│  5. GET /billing/status       → show updated plan/status        │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────────────┐
│  Backend                                                        │
│  • Creates Razorpay subscription (never on client)              │
│  • Verifies HMAC-SHA256 signature (never trust client)          │
│  • Upgrades user plan only after signature verified             │
│  • Receives Razorpay webhooks for redundancy                    │
└────────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────────────┐
│  Razorpay                                                       │
│  • Manages recurring billing automatically                      │
│  • Fires webhooks on every state change                        │
└────────────────────────────────────────────────────────────────┘
```

**Critical rules (never break these):**
- **Never create a Razorpay subscription on the frontend.** Always call `POST /billing/subscribe` first.
- **Never trust the client-reported payment success.** Always call `POST /billing/verify` and wait for `200`.
- **Never upgrade the plan on the frontend** before the verify endpoint returns success.

---

## 2. Environment Setup

### Backend env vars (already configured — for reference)

```bash
Razorpay__Enabled=true                    # false = all billing endpoints return 503
Razorpay__KeyId=rzp_test_XXXXXXXXXXXX     # rzp_live_ in production
Razorpay__KeySecret=YOUR_SECRET           # never expose to frontend
Razorpay__WebhookSecret=YOUR_WEBHOOK_SECRET
```

### Frontend: load Razorpay Checkout script

Add this to your `index.html` or load it dynamically before checkout:

```html
<script src="https://checkout.razorpay.com/v1/checkout.js"></script>
```

Or load dynamically:

```js
function loadRazorpay() {
  return new Promise(resolve => {
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.onload = resolve;
    document.body.appendChild(script);
  });
}
```

---

## 3. Plans API

```
GET /api/v1/billing/plans
```

No auth required. Use this to render the pricing/plan selection page.

**Response:**
```json
[
  {
    "id": "10000000-0000-0000-0000-000000000001",
    "code": "free",
    "name": "Free",
    "priceInrPaise": 0,
    "priceInrRupees": 0,
    "billingPeriod": "monthly",
    "maxInstagramAccounts": 1,
    "maxAutomations": 3,
    "monthlyAutomationRuns": 500,
    "hasRazorpayPlan": false,
    "isFree": true
  },
  {
    "id": "10000000-0000-0000-0000-000000000002",
    "code": "starter",
    "name": "Starter",
    "priceInrPaise": 9900,
    "priceInrRupees": 99,
    "billingPeriod": "monthly",
    "maxInstagramAccounts": 2,
    "maxAutomations": 10,
    "monthlyAutomationRuns": 2000,
    "hasRazorpayPlan": true,
    "isFree": false
  },
  {
    "id": "10000000-0000-0000-0000-000000000003",
    "code": "pro",
    "name": "Pro",
    "priceInrPaise": 29900,
    "priceInrRupees": 299,
    "billingPeriod": "monthly",
    "maxInstagramAccounts": 5,
    "maxAutomations": 50,
    "monthlyAutomationRuns": 10000,
    "hasRazorpayPlan": true,
    "isFree": false
  }
]
```

**UI logic:**
- Show "Current Plan" badge on the plan matching the user's active subscription
- Disable the subscribe button on `isFree: true` plans
- Disable subscribe if `hasRazorpayPlan: false` (plan not linked to Razorpay yet)
- Show `₹{priceInrRupees}/month` for display

---

## 4. Subscription Checkout Flow

### Step 1 — Create subscription on server

```
POST /api/v1/billing/subscribe
Authorization: Bearer <accessToken>
Content-Type: application/json

{ "planCode": "starter" }
```

**Response:**
```json
{
  "succeeded": true,
  "value": {
    "razorpayKeyId": "rzp_test_XXXXXXXXXXXX",
    "razorpaySubscriptionId": "sub_XXXXXXXXXXXXXXXXXX",
    "planName": "Starter",
    "amountInrPaise": 9900,
    "userEmail": "user@example.com",
    "userFullName": "kromic.in"
  }
}
```

### Step 2 — Open Razorpay Checkout

```js
async function startCheckout(planCode) {
  await loadRazorpay();

  // Step 1: create subscription on server
  const res = await fetch('/api/v1/billing/subscribe', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${getAccessToken()}`
    },
    body: JSON.stringify({ planCode })
  });

  const data = await res.json();
  if (!data.succeeded) {
    showError(data.error);
    return;
  }

  const { razorpayKeyId, razorpaySubscriptionId, planName, userEmail, userFullName } = data.value;

  // Step 2: open Razorpay Checkout
  const rzp = new Razorpay({
    key: razorpayKeyId,
    subscription_id: razorpaySubscriptionId,
    name: 'KromicFlow',
    description: `${planName} Plan`,
    image: 'https://flow.kromic.in/logo.png',
    prefill: {
      email: userEmail,
      name: userFullName
    },
    theme: { color: '#00C2FF' },

    handler: async function(response) {
      // Step 3: verify payment on server — NEVER skip this
      await verifyPayment(
        response.razorpay_payment_id,
        response.razorpay_subscription_id,
        response.razorpay_signature
      );
    },

    modal: {
      ondismiss: function() {
        // User closed checkout without paying — no action needed
        console.log('Checkout dismissed');
      }
    }
  });

  rzp.open();
}
```

---

## 5. Verify Payment

**Always call this after Razorpay's `handler` fires. Never skip.**

```
POST /api/v1/billing/verify
Authorization: Bearer <accessToken>
Content-Type: application/json

{
  "razorpayPaymentId": "pay_XXXXXXXXXXXXXXXXXX",
  "razorpaySubscriptionId": "sub_XXXXXXXXXXXXXXXXXX",
  "razorpaySignature": "abc123..."
}
```

**Response on success:**
```json
{
  "succeeded": true,
  "value": {
    "subscriptionId": "sub_XXXXXXXXXXXXXXXXXX",
    "status": "authenticated",
    "planCode": "starter",
    "planName": "Starter"
  }
}
```

**Response on invalid signature:**
```json
{
  "succeeded": false,
  "error": "Payment signature verification failed."
}
```

```js
async function verifyPayment(paymentId, subscriptionId, signature) {
  const res = await fetch('/api/v1/billing/verify', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${getAccessToken()}`
    },
    body: JSON.stringify({
      razorpayPaymentId: paymentId,
      razorpaySubscriptionId: subscriptionId,
      razorpaySignature: signature
    })
  });

  const data = await res.json();

  if (!data.succeeded) {
    showError('Payment verification failed. Please contact support.');
    return;
  }

  // Plan upgrade is done server-side — refresh status
  showSuccess(`You're now on the ${data.value.planName} plan!`);
  await refreshSubscriptionStatus();
}
```

---

## 6. Subscription Status

```
GET /api/v1/billing/status
Authorization: Bearer <accessToken>
```

**Response:**
```json
{
  "hasActiveSubscription": true,
  "subscriptionId": "sub_XXXXXXXXXXXXXXXXXX",
  "status": "active",
  "planCode": "starter",
  "planName": "Starter",
  "priceInrPaise": 9900,
  "priceInrRupees": 99,
  "billingPeriod": "monthly",
  "currentPeriodStart": "2026-07-01T00:00:00Z",
  "currentPeriodEnd": "2026-08-01T00:00:00Z",
  "cancelledAtUtc": null,
  "activatedAtUtc": "2026-07-20T06:24:58Z",
  "paidCount": 1,
  "totalCount": 0,
  "willCancelAtCycleEnd": false
}
```

| Field | UI usage |
|---|---|
| `hasActiveSubscription` | Show/hide upgrade CTA |
| `status` | Badge: `active`=green, `halted`=red, `cancelled`=orange |
| `currentPeriodEnd` | "Next billing date: Aug 1, 2026" |
| `willCancelAtCycleEnd` | "Your plan cancels on Aug 1, 2026" banner |
| `paidCount` | "1 payment made" |
| `willCancelAtCycleEnd` | Show "Resubscribe" button when true |

**Status values:**

| Status | Meaning | UI |
|---|---|---|
| `created` | Subscription created, awaiting auth payment | "Complete payment" prompt |
| `authenticated` | Auth payment done, first charge pending | "Active (starting soon)" |
| `active` | Billing active, charges happening | Green "Active" badge |
| `pending` | Payment attempted but not confirmed | "Payment pending" warning |
| `halted` | Payment failed after all retries | Red "Payment failed" alert |
| `cancelled` | Cancelled (may still be in period) | Orange "Cancels on {date}" |
| `completed` | All billing cycles exhausted | Grey "Expired" |
| `expired` | Expired | Grey "Expired" |

---

## 7. Cancel Subscription

```
POST /api/v1/billing/cancel
Authorization: Bearer <accessToken>
Content-Type: application/json

{ "cancelAtCycleEnd": true }
```

- `cancelAtCycleEnd: true` — user keeps access until current period ends (recommended)
- `cancelAtCycleEnd: false` — access revoked immediately, plan downgraded to Free now

**Response:**
```json
{ "succeeded": true }
```

```js
async function cancelSubscription(immediately = false) {
  const confirmed = await showConfirmDialog(
    immediately
      ? 'Cancel now? You will lose access immediately.'
      : 'Cancel at end of period? You keep access until the billing date.'
  );
  if (!confirmed) return;

  const res = await fetch('/api/v1/billing/cancel', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${getAccessToken()}`
    },
    body: JSON.stringify({ cancelAtCycleEnd: !immediately })
  });

  const data = await res.json();
  if (data.succeeded) {
    showSuccess(immediately ? 'Subscription cancelled.' : 'Subscription will cancel at period end.');
    await refreshSubscriptionStatus();
  }
}
```

---

## 8. Razorpay Webhook Events

The backend receives webhook events at `POST /api/v1/webhooks/razorpay`.

**Configure in Razorpay Dashboard:**
```
URL:    https://flowapi.kromic.in/api/v1/webhooks/razorpay
Secret: (must match Razorpay__WebhookSecret env var)
Events: ✓ subscription.activated
        ✓ subscription.charged
        ✓ subscription.halted
        ✓ subscription.cancelled
        ✓ subscription.completed
        ✓ subscription.expired
        ✓ payment.failed
```

**What happens on each event:**

| Event | Backend action | Frontend impact |
|---|---|---|
| `subscription.activated` | User plan upgraded | Refresh `/billing/status` |
| `subscription.charged` | Paid count incremented | Refresh `/billing/status` |
| `subscription.halted` | Logged as warning | Show "payment failed" alert |
| `subscription.cancelled` | User downgraded to Free | Refresh `/billing/status` |
| `subscription.completed` | User downgraded to Free | Refresh `/billing/status` |
| `subscription.expired` | User downgraded to Free | Refresh `/billing/status` |
| `payment.failed` | Logged | Show retry prompt if needed |

**Frontend polling recommendation:** After any subscription action, poll `/billing/status` every 5 seconds for up to 30 seconds to catch webhook-driven status changes:

```js
async function pollUntilActive(maxAttempts = 6) {
  for (let i = 0; i < maxAttempts; i++) {
    await sleep(5000);
    const status = await fetchSubscriptionStatus();
    if (status.hasActiveSubscription) return status;
  }
  return null;
}
```

---

## 9. UI Screen Reference

### Pricing / Plan Selection Page (public, no auth)

```
GET /api/v1/billing/plans
```

1. Display all 3 plans as cards (Free, Starter ₹99, Pro ₹299)
2. Highlight current plan with "Current Plan" badge (compare with `/billing/status`)
3. "Upgrade" button calls `startCheckout(planCode)`
4. Disable button if `hasRazorpayPlan: false` or plan is already active

### Billing / Settings Page (auth required)

```
GET /api/v1/billing/status
```

Show:
- Current plan name + price
- Status badge
- `currentPeriodEnd` as "Next billing date" (or "Access until" if cancelling)
- "Cancel Subscription" button (only when `hasActiveSubscription: true`)
- "Resubscribe" button (only when `willCancelAtCycleEnd: true` or `status === 'expired'`)
- `paidCount` payments made

### Post-Checkout Success Page

After `verifyPayment` succeeds:
1. Show "Welcome to {planName}!" success state
2. Call `GET /billing/status` to get updated plan details
3. Redirect to dashboard after 3 seconds

---

## 10. Error States

| Error | Cause | UI action |
|---|---|---|
| `Payment processing is not enabled.` | `Razorpay__Enabled=false` on backend | Hide all billing UI, show "Coming soon" |
| `Plan not found.` | Invalid plan code sent | Refresh plan list |
| `Free plan does not require payment.` | Tried to subscribe to Free | Don't show subscribe button on Free plan |
| `This plan is not configured for payments.` | `RazorpayPlanId` not set in DB | Disable plan button, show "Contact support" |
| `You already have an active subscription.` | Double-click protection | Show "Already subscribed" and refresh status |
| `Payment signature verification failed.` | Tampered response or wrong keys | Show "Payment failed, contact support" — do NOT grant access |
| `No active subscription found.` | Cancel called with no active sub | Refresh subscription status |

---

## 11. Test Mode vs Live Mode

### Test mode (development)

Set in backend:
```
Razorpay__KeyId=rzp_test_XXXXXXXXXXXX
```

Use Razorpay test cards:
| Card | Number | CVV | Expiry |
|---|---|---|---|
| Success | 4111 1111 1111 1111 | Any 3 digits | Any future date |
| Failure | 4000 0000 0000 0002 | Any 3 digits | Any future date |
| UPI (success) | success@razorpay | — | — |
| UPI (failure) | failure@razorpay | — | — |

Test subscriptions auto-charge in Razorpay test mode — no real money is moved.

### Switching to live

1. Replace `rzp_test_` keys with `rzp_live_` keys in env vars
2. Set a new `Razorpay__WebhookSecret` from the live webhook configuration
3. Update `Plan.RazorpayPlanId` in the DB to point to live Razorpay plan IDs
4. No code changes required

```bash
# Production env vars
Razorpay__Enabled=true
Razorpay__KeyId=rzp_live_XXXXXXXXXXXX
Razorpay__KeySecret=YOUR_LIVE_SECRET
Razorpay__WebhookSecret=YOUR_LIVE_WEBHOOK_SECRET
```

---

## 12. Security Rules

1. **Never expose `KeySecret` or `WebhookSecret` to the frontend** — these are backend-only
2. **Always verify the payment signature** (`POST /billing/verify`) before granting access
3. **Always verify webhook signatures** — the backend does this automatically
4. **Never grant plan upgrade based on client-reported success** — wait for server verify response
5. **Always use HTTPS** — Razorpay rejects http:// callback/webhook URLs in production
6. **Use the webhook as the ground truth** — the verify endpoint is for immediate UX feedback; the webhook is the authoritative state

---

## 13. Setting Up Razorpay Plans in the Dashboard

Before any user can subscribe, you must create plans in Razorpay and link them to your DB plans.

### Step 1 — Create plans in Razorpay Dashboard

1. Go to [Razorpay Dashboard](https://dashboard.razorpay.com) → Subscriptions → Plans
2. Create "Starter" plan: ₹99, monthly
3. Create "Pro" plan: ₹299, monthly
4. Note the `plan_xxxx` IDs for each

### Step 2 — Update DB via admin endpoint

Use the admin API to update the `RazorpayPlanId` on each plan:

```
POST /api/v1/admin/plans
Authorization: Bearer <adminToken>

{
  "code": "starter",
  "name": "Starter",
  "isActive": true,
  "isDefault": false,
  "maxInstagramAccounts": 2,
  "maxAutomations": 10,
  "monthlyAutomationRuns": 2000,
  "monthlyEmails": 100,
  "monthlyPushNotifications": 100,
  "razorpayPlanId": "plan_XXXXXXXXXXXXXXX"
}
```

Repeat for "pro" plan.

Once `razorpayPlanId` is set, `GET /billing/plans` will return `hasRazorpayPlan: true` and the subscribe button will activate.

---

*Generated: 2026-07-20 · KromicFlow Billing v1*
