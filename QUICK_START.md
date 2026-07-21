# 🚀 Quick Start Guide

## For Developers (Local Setup)

### 1. Clone & Setup (30 seconds)
```powershell
git pull origin main
dotnet build
```

### 2. Database (15 seconds)
```powershell
dotnet ef database update --project src/KromicFlow.Infrastructure --startup-project src/KromicFlow.Api
```

### 3. Configure `.env` (1 minute)
```env
DATABASE_CONNECTION_STRING=Server=localhost;Database=kromic_flow_dev;Trusted_Connection=true;
RAZORPAY_ENABLED=false
```

### 4. Run App (5 seconds)
```powershell
cd src/KromicFlow.Api
dotnet run
```

### 5. Verify (10 seconds)
```powershell
# In new terminal
curl http://localhost:5000/api/v1/billing/plans

# Expected: List of Free, Starter, Pro plans
```

---

## For Frontend Developers

### Plans API
```javascript
// Get all plans
const response = await fetch('https://api.example.com/api/v1/billing/plans');
const plans = await response.json();
// Returns: [{ id, code: 'FREE'|'STARTER'|'PRO', name, priceInrPaise }, ...]
```

### Subscribe to Plan
```javascript
// Step 1: Create subscription
const sub = await fetch('https://api.example.com/api/v1/billing/subscribe', {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ planId: selectedPlanId })
});
const { subscriptionId, keyId } = await sub.json();

// Step 2: Open Razorpay Checkout
const options = {
  key: keyId,           // rzp_test_... or rzp_live_...
  subscription_id: subscriptionId,
  name: 'KromicFlow',
  description: 'Instagram Automation Subscription',
  handler: async (response) => {
    // Step 3: Verify on backend
    const verify = await fetch('https://api.example.com/api/v1/billing/verify', {
      method: 'POST',
      headers: { 
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        razorpayPaymentId: response.razorpay_payment_id,
        razorpaySubscriptionId: response.razorpay_subscription_id,
        razorpaySignature: response.razorpay_signature
      })
    });
    if (verify.ok) {
      // User subscription activated! Show success screen
    }
  }
};
Razorpay.Checkout.open(options);
```

### Check Subscription Status
```javascript
const status = await fetch('https://api.example.com/api/v1/billing/status', {
  headers: { 'Authorization': `Bearer ${jwtToken}` }
});
const { plan, status: subStatus, currentPeriodEnd } = await status.json();
// Shows: { plan: 'STARTER', status: 'active'|'inactive'|'halted', currentPeriodEnd: '2026-08-20T...' }
```

### Cancel Subscription
```javascript
const cancel = await fetch('https://api.example.com/api/v1/billing/cancel', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${jwtToken}` }
});
// Cancels at end of current billing cycle
```

---

## API Endpoints

| Endpoint | Method | Auth | Response |
|----------|--------|------|----------|
| `/api/v1/billing/plans` | GET | None | `[{ id, code, name, priceInrPaise }]` |
| `/api/v1/billing/subscribe` | POST | JWT | `{ subscriptionId, keyId }` |
| `/api/v1/billing/verify` | POST | JWT | `{ status: 'active' }` |
| `/api/v1/billing/status` | GET | JWT | `{ plan, status, currentPeriodEnd }` |
| `/api/v1/billing/cancel` | POST | JWT | `{ status: 'cancelled' }` |
| `/api/v1/webhooks/razorpay` | POST | HMAC | (internal use) |

---

## Env Vars

### Development
```env
RAZORPAY_ENABLED=false
DATABASE_CONNECTION_STRING=...
```

### Production
```env
RAZORPAY_ENABLED=true
RAZORPAY_KEY_ID=rzp_live_xxxxx
RAZORPAY_KEY_SECRET=xxxxx
RAZORPAY_WEBHOOK_SECRET=xxxxx
DATABASE_CONNECTION_STRING=...
```

---

## Plans

| Plan | Price | Features |
|------|-------|----------|
| Free | ₹0 | 1 account, 3 automations, 500 runs/month |
| Starter | ₹99 | 2 accounts, 10 automations, 2000 runs/month |
| Pro | ₹299 | 5 accounts, 50 automations, 10000 runs/month |

---

## Data Protection Keys Fix

**What**: OAuth tokens now survive app restarts ✅

**Why**: Keys persist to database instead of disappearing

**Impact**: Users stay authenticated after deployments

**What happens to old tokens**: They won't decrypt (users must re-authenticate once)

---

## Documentation

| Guide | Purpose |
|-------|---------|
| `docs/10-Frontend/00-DeploymentGuide.md` | Deploy to prod, troubleshooting |
| `docs/10-Frontend/01-BillingIntegrationGuide.md` | Frontend integration details |
| `IMPLEMENTATION_SUMMARY.md` | What was built, how, why |
| `QUICK_START.md` | This file |

---

## Verify Everything Works

```powershell
# Build
dotnet build
# Expected: ✅ Build succeeded (18 warnings OK, 0 errors required)

# Tests
dotnet test --no-build
# Expected: ✅ Passed 2/2 tests

# Run
dotnet run
# Expected: ✅ Server starts on http://localhost:5000

# Health check
curl http://localhost:5000/health
# Expected: { "status": "Healthy" }

# Plans endpoint
curl http://localhost:5000/api/v1/billing/plans
# Expected: [{ "id": "...", "code": "FREE", "name": "Free", ... }, ...]
```

---

## Common Issues

| Issue | Fix |
|-------|-----|
| Build fails | `dotnet restore` then `dotnet build` |
| Database error | Run migrations: `dotnet ef database update ...` |
| Tests fail | Check database connection, run migrations |
| Token doesn't work | User needs to re-authenticate via OAuth |
| Webhook invalid | Check `RAZORPAY_WEBHOOK_SECRET` env var |
| 401 on billing endpoints | Ensure JWT in `Authorization: Bearer` header |

---

## Key Files to Know

```
src/KromicFlow.Api/
├── Controllers/
│   ├── BillingController.cs          ← Subscription endpoints
│   └── RazorpayWebhooksController.cs ← Webhook receiver

src/KromicFlow.Infrastructure/
├── External/
│   └── RazorpayClient.cs             ← Razorpay API client
├── Options/
│   └── RazorpayOptions.cs            ← Config
└── Persistence/
    └── KromicFlowDbContext.cs        ← Data Protection keys config

src/KromicFlow.Domain/
└── Entities/
    ├── UserSubscription.cs           ← Subscription entity
    └── Plan.cs                       ← Plan entity (updated)

Migrations/
├── AddDataProtectionKeys             ← Persistent encryption keys
└── AddRazorpaySubscriptions          ← Subscription tables
```

---

## Next Steps

1. **Read full guides**: `docs/10-Frontend/00-DeploymentGuide.md` and `docs/10-Frontend/01-BillingIntegrationGuide.md`
2. **Set up Razorpay account**: Get test keys
3. **Test locally**: Follow API flow above
4. **Deploy to staging**: Test with Razorpay test keys
5. **Deploy to production**: Use live keys, enable `RAZORPAY_ENABLED=true`

---

**Status**: ✅ Ready to go!  
**Questions?** Check the full guides in `/docs/10-Frontend/`
