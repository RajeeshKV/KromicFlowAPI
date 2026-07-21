# KromicFlow API Endpoints Reference

> **Quick lookup for all API endpoints**  
> Last Updated: July 20, 2026

---

## Authentication Endpoints

### POST /api/v1/auth/meta/authorize

Redirect user to Meta OAuth login

**Response**: `302 Redirect` to Meta authorization page

---

### GET /api/v1/auth/meta/callback

OAuth callback endpoint (handled by frontend redirect)

**Query Parameters**:
- `code` (required) — Authorization code from Meta
- `state` (required) — CSRF protection state

**Response**: `200 OK`
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "...",
  "user": {
    "id": "...",
    "email": "...",
    "fullName": "...",
    "role": "User",
    "planCode": "FREE",
    "isActive": true,
    "marketingEmailEnabled": true,
    "marketingPushEnabled": true
  }
}
```

---

### POST /api/v1/auth/refresh

Refresh access token using refresh token

**Body**:
```json
{
  "refreshToken": "..."
}
```

**Response**: `200 OK`
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "...",
  "expiresIn": 3600
}
```

---

### POST /api/v1/auth/logout

Logout current session

**Auth**: Required (JWT)

**Response**: `200 OK`

---

### POST /api/v1/auth/logout-all

Logout from all devices

**Auth**: Required (JWT)

**Response**: `200 OK`

---

## User Endpoints

### GET /api/v1/users/profile

Get current user profile

**Auth**: Required (JWT)

**Response**: `200 OK`
```json
{
  "id": "...",
  "email": "...",
  "fullName": "...",
  "role": "User",
  "planCode": "FREE",
  "isActive": true,
  "marketingEmailEnabled": true,
  "marketingPushEnabled": true
}
```

---

## Instagram Endpoints

### GET /api/v1/instagram/accounts

List all Instagram accounts for current user

**Auth**: Required (JWT)

**Response**: `200 OK`
```json
[
  {
    "id": "...",
    "username": "kromic_test",
    "displayName": "Kromic Test",
    "profilePicture": "https://...",
    "instagramUserId": "123456789",
    "isConnected": true,
    "connectedAtUtc": "2026-07-20T10:00:00Z",
    "tokenStatus": "active",
    "lastSyncUtc": "2026-07-20T10:30:00Z"
  }
]
```

---

### GET /api/v1/instagram/{id}/profile-image

Get profile picture for an Instagram account (image stream)

**Auth**: Required (JWT)

**Response**: `200 OK` with image stream
- Content-Type: `image/jpeg` or `image/png`
- Binary image data

**Error Responses**:
- `404` — Account not found
- `502` — Failed to fetch from Instagram

---

### POST /api/v1/instagram/{id}/connect

Connect/reconnect an Instagram account

**Auth**: Required (JWT)

**Response**: `200 OK`
```json
{
  "success": true
}
```

---

### DELETE /api/v1/instagram/{id}

Disconnect an Instagram account

**Auth**: Required (JWT)

**Response**: `200 OK`

---

### GET /api/v1/instagram/{id}/media

Get media for an Instagram account (paginated)

**Auth**: Required (JWT)

**Query Parameters**:
- `page` (default: 1)
- `pageSize` (default: 20)
- `mediaType` (optional: "Image", "Video", "Carousel", "Reel")
- `search` (optional: search in captions)

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "...",
      "instagramMediaId": "12345",
      "mediaType": "Image",
      "caption": "Beautiful sunset",
      "mediaUrl": "https://...",
      "thumbnailUrl": "https://...",
      "permalink": "https://instagram.com/p/...",
      "postedAtUtc": "2026-07-20T10:00:00Z",
      "likeCount": 42,
      "commentsCount": 3
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8
  }
}
```

---

### POST /api/v1/instagram/{id}/sync

Manually sync an Instagram account (profile + media)

**Auth**: Required (JWT)

**Response**: `200 OK`
```json
{
  "success": true,
  "message": "Sync completed successfully"
}
```

---

## Automation Endpoints

### GET /api/v1/automations

List all automations for current user

**Auth**: Required (JWT)

**Query Parameters**:
- `page` (default: 1)
- `pageSize` (default: 20)
- `instagramAccountId` (optional: filter by account)

**Response**: `200 OK` with pagination

---

### POST /api/v1/automations

Create a new automation

**Auth**: Required (JWT)

**Body**:
```json
{
  "instagramAccountId": "...",
  "name": "Auto Reply to Comments",
  "description": "Reply to comments with default message",
  "triggerType": "comment",
  "keywords": ["hello", "hi"],
  "action": "public_reply",
  "replyMessage": "Thanks for the comment!",
  "isEnabled": true
}
```

---

### GET /api/v1/automations/{id}

Get automation details

**Auth**: Required (JWT)

---

### PUT /api/v1/automations/{id}

Update automation

**Auth**: Required (JWT)

---

### DELETE /api/v1/automations/{id}

Delete automation

**Auth**: Required (JWT)

---

## Billing Endpoints

### GET /api/v1/billing/plans

Get all available plans (public, no auth required)

**Response**: `200 OK`
```json
[
  {
    "id": "...",
    "code": "FREE",
    "name": "Free Plan",
    "priceInrPaise": 0,
    "features": {
      "maxAccounts": 1,
      "maxAutomations": 3,
      "monthlyRuns": 500
    }
  },
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
]
```

---

### POST /api/v1/billing/subscribe

Create a subscription (get checkout details)

**Auth**: Required (JWT)

**Body**:
```json
{
  "planId": "..."
}
```

**Response**: `200 OK`
```json
{
  "subscriptionId": "sub_12345",
  "keyId": "rzp_test_xxxxx",
  "planId": "...",
  "planName": "Starter"
}
```

---

### POST /api/v1/billing/verify

Verify payment signature and activate subscription

**Auth**: Required (JWT)

**Body**:
```json
{
  "razorpayPaymentId": "pay_...",
  "razorpaySubscriptionId": "sub_...",
  "razorpaySignature": "9ef4dffbfd84..."
}
```

**Response**: `200 OK`
```json
{
  "status": "active",
  "plan": "STARTER"
}
```

---

### GET /api/v1/billing/status

Get current subscription status

**Auth**: Required (JWT)

**Response**: `200 OK`
```json
{
  "plan": "STARTER",
  "status": "active",
  "currentPeriodStart": "2026-07-01T00:00:00Z",
  "currentPeriodEnd": "2026-08-01T00:00:00Z",
  "cancelledAtUtc": null,
  "razorpaySubscriptionId": "sub_..."
}
```

---

### POST /api/v1/billing/cancel

Cancel active subscription (at end of cycle)

**Auth**: Required (JWT)

**Response**: `200 OK`
```json
{
  "status": "cancelled",
  "cancelledAtUtc": "2026-07-20T10:00:00Z"
}
```

---

### POST /api/v1/webhooks/razorpay

Receive Razorpay webhook events (internal, called by Razorpay)

**Auth**: HMAC-SHA256 signature only

**Event Types**: subscription.activated, subscription.charged, subscription.halted, subscription.cancelled, payment.failed, etc.

---

## Analytics Endpoints

### GET /api/v1/analytics/dashboard

Get dashboard analytics

**Auth**: Required (JWT)

**Query Parameters**:
- `from` (optional: ISO date)
- `to` (optional: ISO date)

**Response**: `200 OK`
```json
{
  "totalAccounts": 3,
  "totalAutomations": 12,
  "activeAutomations": 8,
  "totalExecutions": 1250,
  "totalComments": 450,
  "averageResponseTime": 2.3
}
```

---

### GET /api/v1/analytics/automations/{id}

Get automation-specific analytics

**Auth**: Required (JWT)

**Query Parameters**:
- `from` (optional)
- `to` (optional)

**Response**: `200 OK`
```json
{
  "automationId": "...",
  "name": "Auto Reply",
  "executionCount": 245,
  "successCount": 240,
  "failureCount": 5,
  "averageResponseTime": 1.2,
  "lastExecutedAtUtc": "2026-07-20T10:00:00Z"
}
```

---

### GET /api/v1/analytics/timeseries

Get daily execution timeseries

**Auth**: Required (JWT)

**Query Parameters**:
- `from` (optional: ISO date)
- `to` (optional: ISO date)

**Response**: `200 OK`
```json
[
  {
    "date": "2026-07-20",
    "executionCount": 45,
    "successCount": 43,
    "failureCount": 2
  },
  {
    "date": "2026-07-21",
    "executionCount": 52,
    "successCount": 51,
    "failureCount": 1
  }
]
```

---

### GET /api/v1/analytics/conversations

Get conversations (comments/DMs)

**Auth**: Required (JWT)

**Query Parameters**:
- `page` (default: 1)
- `pageSize` (default: 20)
- `mediaIgId` (optional: filter by media)

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "...",
      "commentId": "123",
      "text": "Great photo!",
      "commenterUsername": "user123",
      "mediaUrl": "https://...",
      "createdAtUtc": "2026-07-20T10:00:00Z"
    }
  ],
  "pagination": { ... }
}
```

---

## Admin Endpoints

### GET /api/v1/admin/users

List all users (admin only)

**Auth**: Required (JWT + Admin role)

**Query Parameters**:
- `page` (default: 1)
- `pageSize` (default: 20)

---

### POST /api/v1/admin/users/{id}/block

Block a user (admin only)

**Auth**: Required (JWT + Admin role)

---

### POST /api/v1/admin/settings

Update runtime settings (admin only)

**Auth**: Required (JWT + Admin role)

**Body**:
```json
{
  "key": "plans_enabled",
  "value": "true"
}
```

---

## Webhook Endpoints

### POST /api/v1/webhooks/razorpay

Receive Razorpay billing webhooks

**Auth**: HMAC-SHA256 signature

**Body**: Razorpay event payload with signature header

---

### POST /api/v1/webhooks/instagram

Receive Instagram webhook events (comments, mentions)

**Auth**: Token verification

**Body**: Instagram webhook payload

---

## Health Check

### GET /health

Application health check (no auth required)

**Response**: `200 OK`
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "externalServices": "Healthy"
  }
}
```

---

## Status Codes Reference

| Code | Meaning | When Used |
|------|---------|-----------|
| `200` | OK | Request successful |
| `201` | Created | Resource created |
| `400` | Bad Request | Invalid parameters |
| `401` | Unauthorized | Missing/invalid JWT |
| `403` | Forbidden | No permission |
| `404` | Not Found | Resource not found |
| `429` | Too Many Requests | Rate limited |
| `500` | Server Error | Internal error |
| `502` | Bad Gateway | External service error |
| `504` | Gateway Timeout | External service timeout |

---

## Authentication

### JWT Header Format

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Refresh Token

Use the `refreshToken` from login response to get a new access token via POST /api/v1/auth/refresh

---

## Rate Limiting

- Most endpoints: 100 requests/minute per user
- Billing endpoints: 10 requests/minute per user
- Instagram webhook: No limit (trusted source)

**Rate Limit Headers**:
- `X-RateLimit-Limit`: Total requests allowed
- `X-RateLimit-Remaining`: Requests remaining
- `X-RateLimit-Reset`: Timestamp when limit resets

---

## Error Response Format

```json
{
  "error": "Invalid plan ID",
  "code": "PLAN_NOT_FOUND",
  "details": "Plan with id '...' does not exist"
}
```

---

## Documentation Links

- Full API Docs: `docs/08-API/`
- Frontend Integration: `docs/10-Frontend/`
- Error Codes: `docs/08-API/10-ErrorCodes.md`
- Pagination: `docs/08-API/08-Pagination.md`
- Validation: `docs/08-API/09-Validation.md`

---

**Base URL**: https://api.example.com  
**API Version**: v1  
**Last Updated**: July 20, 2026
