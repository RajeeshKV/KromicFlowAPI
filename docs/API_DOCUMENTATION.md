# Instagram Account Management API Documentation

## Overview

This API provides endpoints for managing Instagram Business accounts, including OAuth authentication, account connection/disconnection, synchronization, and multi-account support.

## Authentication

All endpoints require JWT authentication via the `Authorization` header:

```
Authorization: Bearer <your_jwt_token>
```

---

## Endpoints

### 1. Get Instagram Accounts

Retrieves all Instagram accounts for the authenticated user.

**Endpoint:** `GET /api/v1/instagram/accounts`

**Authorization:** Required

**Response:**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "instagramUserId": "17841400000000000",
    "facebookPageId": "123456789012345",
    "username": "brand_one",
    "displayName": "Brand One",
    "profilePicture": "https://example.com/profile.jpg",
    "isConnected": true,
    "connectedAtUtc": "2026-07-19T10:30:00Z",
    "disconnectedAtUtc": null,
    "lastSyncUtc": "2026-07-19T10:25:00Z",
    "tokenExpiresUtc": "2026-09-17T10:30:00Z",
    "lastTokenRefreshUtc": "2026-07-19T10:30:00Z",
    "tokenStatus": "active",
    "refreshRequired": false
  }
]
```

**Error Responses:**

- `401 Unauthorized` - Invalid or missing JWT token

---

### 2. Connect Instagram Account

Enables automation for a specific Instagram account.

**Endpoint:** `POST /api/v1/instagram/{accountId}/connect`

**Authorization:** Required

**Path Parameters:**

- `accountId` (GUID) - The Instagram account ID to connect

**Response:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "instagramUserId": "17841400000000000",
  "facebookPageId": "123456789012345",
  "username": "brand_one",
  "displayName": "Brand One",
  "profilePicture": "https://example.com/profile.jpg",
  "isConnected": true,
  "connectedAtUtc": "2026-07-19T10:30:00Z",
  "disconnectedAtUtc": null,
  "lastSyncUtc": "2026-07-19T10:25:00Z",
  "tokenExpiresUtc": "2026-09-17T10:30:00Z",
  "lastTokenRefreshUtc": "2026-07-19T10:30:00Z",
  "tokenStatus": "active",
  "refreshRequired": false
}
```

**Error Responses:**

- `400 Bad Request` - Account not found, already connected, or token expired
- `401 Unauthorized` - Invalid or missing JWT token

**Behavior:**

- Validates account ownership
- Sets `IsConnected = true`
- Stores `ConnectedAtUtc` timestamp
- Validates token expiry before connecting
- Does NOT subscribe to Meta webhooks (only enables internal automation)

---

### 3. Disconnect Instagram Account

Disables automation for a specific Instagram account.

**Endpoint:** `DELETE /api/v1/instagram/{accountId}`

**Authorization:** Required

**Path Parameters:**

- `accountId` (GUID) - The Instagram account ID to disconnect

**Response:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "instagramUserId": "17841400000000000",
  "facebookPageId": "123456789012345",
  "username": "brand_one",
  "displayName": "Brand One",
  "profilePicture": "https://example.com/profile.jpg",
  "isConnected": false,
  "connectedAtUtc": "2026-07-19T10:30:00Z",
  "disconnectedAtUtc": "2026-07-19T11:00:00Z",
  "lastSyncUtc": "2026-07-19T10:25:00Z",
  "tokenExpiresUtc": "2026-09-17T10:30:00Z",
  "lastTokenRefreshUtc": "2026-07-19T10:30:00Z",
  "tokenStatus": "active",
  "refreshRequired": false
}
```

**Error Responses:**

- `400 Bad Request` - Account not found or already disconnected
- `401 Unauthorized` - Invalid or missing JWT token

**Behavior:**

- Sets `IsConnected = false`
- Stores `DisconnectedAtUtc` timestamp
- Stops all automations for the account
- Does NOT delete the account from the database

---

### 4. Sync Instagram Account

Synchronizes Instagram account data and refreshes tokens.

**Endpoint:** `POST /api/v1/instagram/{accountId}/sync`

**Authorization:** Required

**Path Parameters:**

- `accountId` (GUID) - The Instagram account ID to sync

**Response:**

```json
{
  "succeeded": true
}
```

**Error Responses:**

- `400 Bad Request` - Account not found, token expired, or sync failed
- `401 Unauthorized` - Invalid or missing JWT token

**Behavior:**

- Refreshes profile information (username, display name, profile picture)
- Validates token expiry and refreshes if expiring soon (within 7 days)
- Updates `LastSyncUtc` timestamp
- Updates `LastTokenRefreshUtc` if token was refreshed
- Sets `TokenStatus` to "expired" if refresh fails
- Does NOT automatically connect the account

---

## OAuth Flow

### Instagram OAuth Login

**Endpoint:** `GET /api/v1/auth/login`

**Authorization:** Not required

**Query Parameters:**

- `redirect_uri` (optional) - Override redirect URI (defaults to configured value)

**Behavior:**

1. Redirects to Instagram OAuth authorization page
2. User authorizes the application
3. Instagram redirects to configured callback URL

**Scopes:**

- `instagram_business_basic` - Basic profile information
- `instagram_business_manage_messages` - DM management
- `instagram_business_manage_comments` - Comment management

---

### OAuth Callback

**Endpoint:** `GET /api/v1/auth/callback`

**Authorization:** Not required

**Query Parameters:**

- `code` - Instagram authorization code
- `state` - OAuth state parameter for CSRF protection

**Behavior:**

1. Exchanges authorization code for short-lived token
2. Converts to long-lived token (60 days)
3. Retrieves all Facebook Pages managed by user
4. Retrieves Instagram Business accounts linked to each page
5. Upserts all discovered accounts to database
6. Marks newly discovered accounts as disconnected by default
7. Redirects to frontend with tokens in URL fragment

**Frontend Redirect:**

```
{FrontendRedirectUri}#accessToken={token}&refreshToken={refresh}&expiresUtc={expiry}&sessionGuid={guid}&userId={userId}&email={email}&fullName={name}&role={role}
```

**Error Redirect:**

```
{FrontendRedirectUri}?error={error_message}
```

---

## Multi-Account Support

The system fully supports managing multiple Instagram Business accounts:

- **Independent connection state** - Each account can be connected/disconnected independently
- **Independent automations** - Automations are scoped to individual accounts
- **Independent analytics** - Analytics are tracked per account
- **Independent quotas** - Usage quotas are tracked per account
- **Independent synchronization** - Each account can be synced independently
- **Independent webhook processing** - Webhooks are filtered by account connection state

---

## Webhook Processing

Webhook events are automatically filtered based on account connection state:

1. Extract Instagram account ID from webhook payload
2. Look up account in database
3. Check if account is connected
4. Only process webhooks from connected accounts
5. Ignore events from disconnected accounts

---

## Token Health Monitoring

Each Instagram account tracks token health:

- `TokenExpiresUtc` - When the access token expires
- `LastTokenRefreshUtc` - When the token was last refreshed
- `TokenStatus` - Current token status (`active`, `expired`, `revoked`, `invalid`)

The frontend can use this information to notify users before token expiration.

---

## Duplicate Account Prevention

The database enforces unique constraints to prevent duplicate accounts:

- `InstagramUserId` - Unique Instagram account ID
- `FacebookPageId` - Unique Facebook Page ID

OAuth login updates existing records instead of creating duplicates.

---

## Error Handling

All endpoints follow consistent error handling:

```json
{
  "succeeded": false,
  "error": "Error message describing what went wrong"
}
```

Common error messages:

- "Instagram account not found."
- "Account is already connected."
- "Account is already disconnected."
- "Account access token has expired. Please re-authenticate."
- "Account does not belong to the current user."
- "Account is not connected. Please connect the account to enable automations."
- "User account is not active."
- "User account has login restrictions."

---

## Rate Limiting

Consider implementing rate limiting for:

- OAuth callback endpoint (prevent abuse)
- Sync endpoint (prevent excessive API calls)
- Connect/Disconnect endpoints (prevent rapid state changes)

---

## Security Considerations

1. **Token Storage** - Access tokens are encrypted using data protection
2. **OAuth State** - CSRF protection via state parameter validation
3. **Webhook Verification** - HMAC signature verification for webhook authenticity
4. **Ownership Validation** - All operations validate account ownership
5. **Connection State** - Only connected accounts process webhooks and automations

---

## Configuration

Required configuration settings:

```json
{
  "Meta": {
    "AppId": "YOUR_INSTAGRAM_APP_ID",
    "AppSecret": "YOUR_INSTAGRAM_APP_SECRET",
    "OAuthRedirectUri": "http://localhost:5000/api/v1/auth/callback",
    "FrontendRedirectUri": "http://localhost:3000/auth/callback",
    "WebhookVerifyToken": "YOUR_WEBHOOK_VERIFY_TOKEN",
    "WebhookAppSecret": "YOUR_WEBHOOK_APP_SECRET",
    "GraphApiBaseUrl": "https://graph.instagram.com/v20.0",
    "ApiBaseUrl": "https://api.instagram.com"
  }
}
```

---

## Instagram API References

- [Instagram Graph API Documentation](https://developers.facebook.com/docs/instagram-platform/instagram-api-with-instagram-login/business-login/)
- [Instagram Basic Display API](https://developers.facebook.com/docs/instagram-basic-display-api)
- [Instagram Graph API](https://developers.facebook.com/docs/instagram-api)
