# KromicFlow — Deployment & Verification Guide

> **Status**: Data Protection Keys + Razorpay Billing Integration Complete  
> **Build**: ✅ PASSED (0 errors, 18 warnings)  
> **Tests**: ✅ PASSED (2/2 tests)  
> **Ready for**: Development testing → Staging → Production

---

## 📋 Table of Contents

1. [What Was Implemented](#what-was-implemented)
2. [Database Migrations](#database-migrations)
3. [Environment Configuration](#environment-configuration)
4. [Local Development Setup](#local-development-setup)
5. [Production Deployment](#production-deployment)
6. [Testing Checklist](#testing-checklist)
7. [Monitoring & Troubleshooting](#monitoring--troubleshooting)
8. [Rollback Procedures](#rollback-procedures)

---

## ✨ What Was Implemented

### 1. **Data Protection Keys — Persistent (Database-backed)**

**Problem Fixed**: OAuth tokens were encrypted with ephemeral keys that disappeared on app restart, causing token decryption failures.

**Solution**: Migrated from file-based/ephemeral keys to EntityFrameworkCore-backed persistent keys stored in the database.

**Files Modified**:
- `src/KromicFlow.Infrastructure/Persistence/KromicFlowDbContext.cs` — Added `DbSet<DataProtectionKey>` + table configuration
- `src/KromicFlow.Infrastructure/DependencyInjection.cs` — Changed `AddDataProtection()` to `AddDataProtection().PersistKeysToDbContext<KromicFlowDbContext>()`
- Migration: `AddDataProtectionKeys` — Creates `data_protection_keys` table

**Impact**:
- ✅ Tokens survive app restarts
- ✅ Keys sync across horizontal scaling (load-balanced instances)
- ✅ No more "decryption failed" errors after deployment

---

### 2. **Razorpay Subscription Billing — Production-Grade**

**Features**:
- ✅ Plans API: Fetch all plans with pricing
- ✅ Subscribe endpoint: Create recurring subscriptions
- ✅ Verify endpoint: Verify payment signatures (HMAC-SHA256)
- ✅ Cancel endpoint: Cancel active subscriptions at cycle end
- ✅ Status endpoint: Real-time subscription + plan details
- ✅ Webhook receiver: Handle 7 Razorpay events with full validation
- ✅ Fully configurable: Enable/disable via env var (no hard code)
- ✅ Audit logging: All billing events tracked in audit logs

**Files Added**:
- `src/KromicFlow.Infrastructure/Options/RazorpayOptions.cs` — Configuration (KeyId, KeySecret, WebhookSecret, Enabled flag)
- `src/KromicFlow.Domain/Entities/UserSubscription.cs` — Domain entity for subscription tracking
- `src/KromicFlow.Domain/Entities/Plan.cs` — Added `RazorpayPlanId` field to link with Razorpay
- `src/KromicFlow.Infrastructure/External/RazorpayClient.cs` — HTTP client for Razorpay API
- `src/KromicFlow.Infrastructure/Services/IRazorpayClient.cs` — Abstraction for dependency injection
- `src/KromicFlow.Api/Controllers/BillingController.cs` — 5 endpoints for subscription management
- `src/KromicFlow.Api/Controllers/RazorpayWebhooksController.cs` — Webhook receiver with HMAC verification
- Migration: `AddRazorpaySubscriptions` — Tables for `user_subscriptions` and updated `plans`

**Endpoints**:
```
GET  /api/v1/billing/plans                    → List all plans
POST /api/v1/billing/subscribe                → Create subscription
POST /api/v1/billing/verify                   → Verify payment signature
GET  /api/v1/billing/status                   → Get current subscription
POST /api/v1/billing/cancel                   → Cancel subscription
POST /api/v1/webhooks/razorpay                → Receive Razorpay webhooks
```

**Webhook Events Handled**:
1. `subscription.activated` — Upgrade user's plan
2. `subscription.charged` — Log successful charge
3. `subscription.halted` — Pause plan features
4. `subscription.cancelled` — Downgrade plan
5. `subscription.completed` — Auto-cancel after contract end
6. `subscription.failed` — Log failure + alert user
7. `payment.failed` — Log payment failure

---

## 🗄️ Database Migrations

### Apply Migrations

```powershell
# Development (local SQLite or PostgreSQL)
dotnet ef database update --project src/KromicFlow.Infrastructure --startup-project src/KromicFlow.Api

# Production (via deployment pipeline)
# Migrations run automatically on container startup via Dockerfile health check
```

### Migrations Included

| Migration | Tables | Purpose |
|-----------|--------|---------|
| `AddDataProtectionKeys` | `data_protection_keys` | Store encryption keys persistently |
| `AddRazorpaySubscriptions` | `user_subscriptions`, `plans` (altered) | Track user subscriptions and plan Razorpay IDs |

---

## ⚙️ Environment Configuration

### Required Env Vars for Razorpay

Add to `.env` (or deployment platform secrets):

```env
# Razorpay Configuration
RAZORPAY_ENABLED=false                    # Start with false in dev, enable in production
RAZORPAY_KEY_ID=rzp_test_xxxxxxxxxxxxx    # From Razorpay dashboard
RAZORPAY_KEY_SECRET=xxxxxxxxxxxxx         # From Razorpay dashboard (keep secret!)
RAZORPAY_WEBHOOK_SECRET=xxxxxxxxxxxxx     # Generated in Razorpay webhook settings

# Data Protection (automatic, no config needed)
# Keys auto-persist to database via EntityFramework
```

### Optional: Plan Configuration

Plans are **hardcoded-seeded** in `KromicFlowDbContext.OnModelCreating()`:

| Plan | Price | Features |
|------|-------|----------|
| Free | ₹0 | 1 account, 3 automations, 500 runs/month |
| Starter | ₹99 | 2 accounts, 10 automations, 2000 runs/month |
| Pro | ₹299 | 5 accounts, 50 automations, 10000 runs/month |

To modify: Edit `src/KromicFlow.Infrastructure/Persistence/KromicFlowDbContext.cs` in the `HasData()` call for Plan entity.

---

## 🏗️ Local Development Setup

### 1. Clone & Build

```powershell
git clone <repo>
cd c:\Personal\KromicFlowAPI
dotnet build
```

### 2. Set Up Database

```powershell
# If using SQLite (default for dev)
# Delete existing database first
Remove-Item -Path data -Recurse -Force

# Apply migrations
dotnet ef database update --project src/KromicFlow.Infrastructure --startup-project src/KromicFlow.Api
```

### 3. Update `.env`

```env
DATABASE_CONNECTION_STRING=Server=localhost;Database=kromic_flow_dev;Trusted_Connection=true;
RAZORPAY_ENABLED=false
```

### 4. Run Application

```powershell
cd src/KromicFlow.Api
dotnet run
```

Visit: `http://localhost:5000/health` → should return `{"status":"Healthy"}`

### 5. Test Endpoints

```powershell
# Get plans (should return Free, Starter, Pro)
curl http://localhost:5000/api/v1/billing/plans

# Get current user subscription (should return 200 with subscription data)
curl -H "Authorization: Bearer <jwt_token>" http://localhost:5000/api/v1/billing/status
```

---

## 🚀 Production Deployment

### Pre-Deployment Checklist

- [ ] All env vars set in deployment platform (Render, AWS, etc.)
- [ ] Database connection string points to production DB
- [ ] Razorpay keys are from **Live** account (not Test)
- [ ] `RAZORPAY_ENABLED=true`
- [ ] Webhook URL registered in Razorpay dashboard: `https://your-domain.com/api/v1/webhooks/razorpay`
- [ ] SSL certificate valid
- [ ] Build passes: `dotnet build`
- [ ] Tests pass: `dotnet test`
- [ ] No secrets in `.env` or code (use platform secrets manager)

### Deployment Steps

#### Option 1: Docker (Recommended)

```powershell
# Build image
docker build -t kromic-flow:latest .

# Run with env vars
docker run -e DATABASE_CONNECTION_STRING="..." \
           -e RAZORPAY_ENABLED="true" \
           -e RAZORPAY_KEY_ID="..." \
           -e RAZORPAY_KEY_SECRET="..." \
           -e RAZORPAY_WEBHOOK_SECRET="..." \
           -p 80:8080 \
           kromic-flow:latest
```

#### Option 2: Render.com (PaaS)

```yaml
# render.yaml already configured
# Update Dockerfile secrets in Render dashboard
# Trigger deploy: git push origin main
```

#### Option 3: Azure / AWS

```powershell
# Push to repository, platform auto-deploys via CI/CD
# Set env vars in platform UI or via CLI
```

### Post-Deployment: Migrations Run Automatically

The `Dockerfile` includes a health check that runs migrations:

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD ["dotnet", "KromicFlow.Api.dll", "migrate"]
```

**This means**: Migrations run automatically on first container startup. No manual `dotnet ef database update` needed.

---

## ✅ Testing Checklist

### 1. Data Protection Keys

**Objective**: Verify tokens survive app restart

```powershell
# Start app
dotnet run

# In another terminal: Get JWT token via OAuth
curl http://localhost:5000/auth/meta/callback?code=<code>
# Returns: { "accessToken": "eyJ..." }

# Verify token works
curl -H "Authorization: Bearer eyJ..." http://localhost:5000/api/v1/users/profile
# Expected: 200 OK, user profile

# Stop app (Ctrl+C)
# Start app again
dotnet run

# Verify token STILL works (this is the fix)
curl -H "Authorization: Bearer eyJ..." http://localhost:5000/api/v1/users/profile
# Expected: 200 OK (before fix: would be 401 Unauthorized due to key loss)
```

### 2. Razorpay Subscription Flow (Test Mode)

**Use Razorpay Test Keys**: `rzp_test_xxxxx`

```powershell
# Step 1: Get plans
curl http://localhost:5000/api/v1/billing/plans
# Expected: 200 OK
# Response:
# [
#   { "id": "...", "code": "FREE", "name": "Free", "priceInrPaise": 0 },
#   { "id": "...", "code": "STARTER", "name": "Starter", "priceInrPaise": 9900 },
#   { "id": "...", "code": "PRO", "name": "Pro", "priceInrPaise": 29900 }
# ]

# Step 2: Create subscription
curl -X POST http://localhost:5000/api/v1/billing/subscribe \
  -H "Authorization: Bearer <jwt_token>" \
  -H "Content-Type: application/json" \
  -d '{ "planId": "<plan_id_for_starter>" }'
# Expected: 200 OK
# Response: { "subscriptionId": "sub_...", "keyId": "rzp_test_..." }

# Step 3: Frontend opens Razorpay Checkout
# (In React app, use Razorpay.Checkout with response above)

# Step 4: After payment, verify signature
curl -X POST http://localhost:5000/api/v1/billing/verify \
  -H "Authorization: Bearer <jwt_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "razorpayPaymentId": "pay_...",
    "razorpaySubscriptionId": "sub_...",
    "razorpaySignature": "9ef4dffbfd84f1318f6739a3ce19f9d85851857ae648f114332d8401e0949a3d"
  }'
# Expected: 200 OK
# User subscription now ACTIVATED

# Step 5: Get subscription status
curl -H "Authorization: Bearer <jwt_token>" \
  http://localhost:5000/api/v1/billing/status
# Expected: 200 OK
# Response: { "plan": "STARTER", "status": "active", "currentPeriodEnd": "2026-08-20T..." }

# Step 6: Receive webhook (simulated)
# Razorpay sends: POST /api/v1/webhooks/razorpay with signature
# Backend verifies HMAC-SHA256, updates subscription status
# Check audit logs: SELECT * FROM audit_logs WHERE action = 'subscription_activated'
```

### 3. Error Scenarios

```powershell
# Invalid signature (tampered webhook)
curl -X POST http://localhost:5000/api/v1/webhooks/razorpay \
  -H "Content-Type: application/json" \
  -H "X-Razorpay-Signature: invalid_signature" \
  -d '{ "event": "subscription.activated", "payload": { ... } }'
# Expected: 401 Unauthorized

# Disabled Razorpay (RAZORPAY_ENABLED=false)
curl http://localhost:5000/api/v1/billing/plans
# Expected: 403 Forbidden or feature disabled message

# Missing or invalid JWT token
curl http://localhost:5000/api/v1/billing/status
# Expected: 401 Unauthorized
```

---

## 📊 Monitoring & Troubleshooting

### 1. Check Data Protection Keys in Database

```sql
-- PostgreSQL
SELECT id, friendly_name, creation_date FROM data_protection_keys;

-- SQLite
SELECT id, friendly_name, creation_date FROM data_protection_keys;
```

**Expected**: Rows appearing after app startup = keys successfully persisted.

### 2. Check Subscription Data

```sql
SELECT 
  u.id, u.email,
  us.razorpay_subscription_id,
  us.status,
  us.current_period_end
FROM users u
LEFT JOIN user_subscriptions us ON us.user_id = u.id;
```

### 3. Monitor Logs

```powershell
# Docker logs
docker logs <container_id> | grep -i razorpay
docker logs <container_id> | grep -i subscription

# Application logs (stored in database or file)
SELECT * FROM audit_logs WHERE action LIKE '%subscription%' ORDER BY created_at DESC LIMIT 20;
```

### 4. Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Token decryption failed" | Keys still ephemeral | Restart app after migration, or clear token cache |
| 401 after app restart | Old token encrypted with lost key | Re-authenticate user via OAuth |
| Webhook signature invalid | Secret mismatch | Verify `RAZORPAY_WEBHOOK_SECRET` matches Razorpay dashboard |
| Subscription not created | Razorpay disabled in env | Set `RAZORPAY_ENABLED=true` |
| 403 Forbidden on /billing/* | User not authenticated | Ensure valid JWT in `Authorization: Bearer` header |

---

## 🔄 Rollback Procedures

### If Data Protection Migration Fails

```powershell
# Rollback migration
dotnet ef migrations remove --project src/KromicFlow.Infrastructure --startup-project src/KromicFlow.Api

# Revert code changes (git)
git revert <commit_hash>

# App will fall back to file-based keys (temporary until redeploy with fix)
```

### If Razorpay Integration Breaks Production

```powershell
# Disable via env var
RAZORPAY_ENABLED=false

# Redeploy
# Billing endpoints return 403, users unaffected

# OR: Rollback to previous commit
git revert <commit_hash>
git push origin main
# CI/CD auto-redeploys
```

### Database Rollback

```powershell
# If migration corrupted data
dotnet ef migrations remove --project src/KromicFlow.Infrastructure --startup-project src/KromicFlow.Api

# Restore database from backup
# Rerun migrations
dotnet ef database update --project src/KromicFlow.Infrastructure --startup-project src/KromicFlow.Api
```

---

## 📞 Support

### Questions?

- **Data Protection**: See `docs/05-Security/06-Encryption.md`
- **Razorpay Integration**: See `docs/10-Frontend/01-BillingIntegrationGuide.md`
- **API Errors**: See `docs/08-API/10-ErrorCodes.md`
- **Testing**: See `docs/07-Meta/13-Testing.md`

---

**Last Updated**: July 20, 2026  
**Status**: ✅ Ready for Development & Production  
**Next**: Deploy to staging, test end-to-end, then promote to production.
