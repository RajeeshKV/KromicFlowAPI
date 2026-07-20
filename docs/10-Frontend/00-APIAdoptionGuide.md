# KromicFlow API — Frontend Adoption Guide

> **Base URL:** `https://flowapi.kromic.in/api/v1`  
> **Auth:** All endpoints (except `/auth/*`) require `Authorization: Bearer <accessToken>`  
> **Content-Type:** `application/json`

---

## Table of Contents

1. [Authentication Flow](#1-authentication-flow)
2. [Instagram Accounts](#2-instagram-accounts)
3. [Automations](#3-automations)
4. [Analytics — Dashboard Stats](#4-analytics--dashboard-stats)
5. [Analytics — Per-Automation Stats](#5-analytics--per-automation-stats)
6. [Analytics — Conversations](#6-analytics--conversations)
7. [Analytics — Time Series](#7-analytics--time-series)
8. [Media](#8-media)
9. [Webhooks (read-only reference)](#9-webhooks-read-only-reference)
10. [Screen-to-Endpoint Map](#10-screen-to-endpoint-map)
11. [Enum Reference](#11-enum-reference)
12. [Error Handling](#12-error-handling)

---

## 1. Authentication Flow

### Step 1 — Redirect user to Instagram login

```
GET /auth/login
```

No body. Server redirects the browser to Instagram OAuth. After the user authorises, Instagram redirects back to your frontend callback URL with tokens in the URL hash fragment.

### Step 2 — Handle callback

The callback URL will look like:

```
https://flow.kromic.in/auth/callback#accessToken=...&refreshToken=...&expiresUtc=...&sessionGuid=...&userId=...&fullName=...&role=...
```

Parse the hash fragment and store the tokens in memory (or a secure cookie). Do **not** store in `localStorage`.

| Fragment param | Type | Description |
|---|---|---|
| `accessToken` | string | Short-lived JWT — include in every API call |
| `refreshToken` | string | Used to get a new access token |
| `expiresUtc` | ISO 8601 | When the access token expires |
| `sessionGuid` | UUID | Identifies this session — needed for logout |
| `userId` | UUID | Your app's user ID |
| `fullName` | string | User's Instagram username |
| `role` | string | `User` or `Admin` |

### Step 3 — Refresh the access token

Call this **before** the access token expires (check `expiresUtc`).

```
POST /auth/refresh
```

```json
{
  "refreshToken": "abc123...",
  "sessionGuid": "e4466f13-a1b7-..."
}
```

**Response:**
```json
{
  "succeeded": true,
  "value": {
    "tokens": {
      "accessToken": "eyJ...",
      "refreshToken": "xyz...",
      "expiresUtc": "2026-07-21T06:00:00Z",
      "sessionGuid": "e4466f13-..."
    },
    "profile": { "id": "...", "fullName": "kromic.in", "role": "User" }
  }
}
```

### Logout

```
POST /auth/logout
```
Invalidates the current session.

```
POST /auth/logout-all
```
Invalidates all sessions for this user (all devices).

---

## 2. Instagram Accounts

### List connected accounts

```
GET /instagram/accounts
```

**Response:**
```json
[
  {
    "id": "255470f3-538a-4109-83a1-776a0e862166",
    "instagramUserId": "17841418137586117",
    "username": "kromic.in",
    "displayName": "kromic.in",
    "profilePicture": "https://...",
    "isConnected": true,
    "tokenStatus": "active",
    "connectedAtUtc": "2026-07-20T05:49:00Z",
    "lastSyncUtc": "2026-07-20T06:00:00Z"
  }
]
```

> **Important:** Store `id` (UUID) as `instagramAccountId`. This is the value you pass to every subsequent analytics/automations call — **not** `instagramUserId`.

| Field | Use |
|---|---|
| `id` | Pass as `instagramAccountId` in all queries |
| `username` | Display in the account selector dropdown |
| `profilePicture` | Avatar in the sidebar |
| `isConnected` | Show a "disconnected" warning badge if `false` |
| `tokenStatus` | `active` \| `expired` \| `invalid` — show re-auth prompt if not `active` |

### Sync media for an account

```
POST /instagram/{id}/sync
```

Triggers a fresh pull of media from Instagram. Call this when the user manually refreshes or after connecting an account.

---

## 3. Automations

### List all automations

```
GET /automations
```

Returns all automations across all accounts for the logged-in user.

**Response:**
```json
[
  {
    "id": "1b3f19fd-21c1-40f4-865e-630e899d894d",
    "instagramAccountId": "255470f3-538a-4109-83a1-776a0e862166",
    "name": "Kromic Flow",
    "scope": 0,
    "triggerType": 1,
    "keywords": [],
    "publicReply": "Sent you a DM",
    "privateReply": "https://flow.kromic.in/",
    "sendPublicReply": true,
    "sendPrivateReply": true,
    "enabled": true,
    "cooldownSeconds": 60,
    "priority": 1,
    "selectedMedia": [
      {
        "id": "681f88f1-...",
        "instagramMediaId": "18004456880763024",
        "caption": "Still running your small...",
        "thumbnailUrl": "",
        "mediaUrl": "https://cdn.instagram.com/...",
        "postedAtUtc": "2026-07-17T03:48:45Z"
      }
    ]
  }
]
```

> Show `mediaUrl` when `thumbnailUrl` is empty — images never have a `thumbnailUrl` (only videos do).

### Create automation

```
POST /automations
```

```json
{
  "instagramAccountId": "255470f3-538a-4109-83a1-776a0e862166",
  "name": "My Automation",
  "scope": 0,
  "triggerType": 1,
  "keywords": [],
  "publicReply": "Sent you a DM",
  "privateReply": "https://flow.kromic.in/",
  "sendPublicReply": true,
  "sendPrivateReply": true,
  "cooldownSeconds": 60,
  "priority": 1,
  "selectedMediaIds": ["681f88f1-3600-4862-9412-6d13e5020da5"]
}
```

| Field | Type | Notes |
|---|---|---|
| `scope` | int | `0=SpecificPosts`, `1=ExistingPosts`, `2=FuturePosts`, `3=AllPosts` |
| `triggerType` | int | `0=CommentKeyword`, `1=AnyComment` |
| `keywords` | string[] | Required when `triggerType=0`. Empty array when `triggerType=1` |
| `sendPublicReply` | bool | Whether to post a public comment reply |
| `sendPrivateReply` | bool | Whether to send a private DM |
| `cooldownSeconds` | int | Min seconds between automation fires for the same commenter (0 = no cooldown) |
| `priority` | int | Lower number = higher priority. Matters when multiple automations match |
| `selectedMediaIds` | UUID[] | Required when `scope=0 (SpecificPosts)`. Use internal `id` from the media list |

Returns the created automation in the same shape as the list item.

### Update automation

```
PUT /automations/{id}
```

Same body as create (without `instagramAccountId`). Returns updated automation.

### Toggle enabled

```
PATCH /automations/{id}/enable
```

```json
{ "enabled": false }
```

### Delete automation

```
DELETE /automations/{id}
```

---

## 4. Analytics — Dashboard Stats

```
GET /analytics/dashboard
```

**Query params:**

| Param | Type | Required | Description |
|---|---|---|---|
| `instagramAccountId` | UUID | No | Scope to one account. Omit for all accounts |
| `from` | ISO 8601 UTC | No | Start of date range |
| `to` | ISO 8601 UTC | No | End of date range |

**Response:**
```json
{
  "activeAutomations": 2,
  "totalAutomations": 3,
  "executionsToday": 12,
  "totalExecutions": 127,
  "publicRepliesSent": 110,
  "privateRepliesSent": 98,
  "successCount": 120,
  "failedCount": 7,
  "skippedCount": 15,
  "successRate": 94.5
}
```

**Dashboard card mapping:**

| UI card | Field |
|---|---|
| Active Automations | `activeAutomations` / `totalAutomations` |
| Executions Today | `executionsToday` |
| Messages Sent | `publicRepliesSent + privateRepliesSent` |
| Success Rate | `successRate` + `"%"` |

---

## 5. Analytics — Per-Automation Stats

```
GET /analytics/automations?instagramAccountId={uuid}
```

**Query params:**

| Param | Type | Required | Description |
|---|---|---|---|
| `instagramAccountId` | UUID | Yes | Which account's automations to stat |
| `from` | ISO 8601 UTC | No | Start of date range |
| `to` | ISO 8601 UTC | No | End of date range |

**Response:**
```json
[
  {
    "automationId": "1b3f19fd-...",
    "automationName": "Kromic Flow",
    "enabled": true,
    "totalExecutions": 80,
    "successCount": 78,
    "failedCount": 2,
    "publicRepliesSent": 78,
    "privateRepliesSent": 70,
    "successRate": 97.5,
    "lastFiredUtc": "2026-07-20T06:24:58Z"
  }
]
```

**Usage:** Join with `GET /automations` on `automationId = id` to enrich automation cards with live stats.

```
"Total: 80 · Success: 97.5% · Last fired: 2 hours ago"
```

---

## 6. Analytics — Conversations

```
GET /analytics/conversations?instagramAccountId={uuid}
```

**Query params:**

| Param | Type | Required | Description |
|---|---|---|---|
| `instagramAccountId` | UUID | Yes | Which account's conversations |
| `mediaIgId` | string | No | Filter to a specific Instagram post |
| `page` | int | No | Page number (default: 1) |
| `pageSize` | int | No | Items per page (default: 25, max: 100) |

**Response:**
```json
{
  "items": [
    {
      "commenterIgId": "1699952474555442",
      "commenterUsername": "rajeesh__kv",
      "latestCommentText": "❤️",
      "mediaIgId": "18004456880763024",
      "receivedUtc": "2026-07-20T06:24:53Z",
      "status": "Completed",
      "publicReplySent": true,
      "privateReplySent": true,
      "totalInteractions": 5
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 42
}
```

| Field | UI usage |
|---|---|
| `commenterUsername` | Conversation title / avatar placeholder |
| `latestCommentText` | Message preview in the list |
| `receivedUtc` | Timestamp — format as relative ("2 min ago") |
| `status` | Badge: `Completed`=green, `Failed`=red, `Pending`=yellow |
| `publicReplySent` | Show "Replied" badge |
| `privateReplySent` | Show "DM Sent" badge |
| `totalInteractions` | "5 interactions" count |

**Pagination:** Total pages = `Math.ceil(totalCount / pageSize)`.

---

## 7. Analytics — Time Series

```
GET /analytics/timeseries?instagramAccountId={uuid}&from={date}&to={date}
```

**Query params:**

| Param | Type | Required | Description |
|---|---|---|---|
| `instagramAccountId` | UUID | Yes | Account to query |
| `from` | ISO 8601 UTC | Yes | Start date |
| `to` | ISO 8601 UTC | Yes | End date (max 90 days from `from`) |
| `automationId` | UUID | No | Scope to a single automation |

**Response:**
```json
[
  {
    "date": "2026-07-01T00:00:00Z",
    "totalExecutions": 15,
    "successCount": 14,
    "failedCount": 1,
    "publicRepliesSent": 14,
    "privateRepliesSent": 12
  },
  {
    "date": "2026-07-02T00:00:00Z",
    "totalExecutions": 0,
    "successCount": 0,
    "failedCount": 0,
    "publicRepliesSent": 0,
    "privateRepliesSent": 0
  }
]
```

Every day in the range is returned — zero-filled when no activity occurred. Feed directly into a chart library (Chart.js, Recharts, etc.).

**Recommended usage for the Analytics screen:**

```js
// Last 30 days
const from = new Date();
from.setDate(from.getDate() - 30);

GET /analytics/timeseries
  ?instagramAccountId=255470f3-...
  &from=2026-06-20T00:00:00Z
  &to=2026-07-20T23:59:59Z
```

---

## 8. Media

Used when creating automations with `scope=0 (SpecificPosts)`.

```
GET /instagram/{instagramAccountId}/media
```

**Query params:**

| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page |
| `mediaType` | string | — | `Image`, `Video`, `Carousel`, `Reel` |
| `search` | string | — | Filter by caption text |

**Response:**
```json
{
  "items": [
    {
      "id": "681f88f1-3600-4862-9412-6d13e5020da5",
      "instagramMediaId": "18004456880763024",
      "mediaType": "Image",
      "caption": "Still running your small...",
      "thumbnailUrl": "",
      "mediaUrl": "https://cdn.instagram.com/...",
      "permalink": "https://www.instagram.com/p/...",
      "postedAtUtc": "2026-07-17T03:48:45Z",
      "likeCount": 12,
      "commentsCount": 4
    }
  ],
  "total": 18
}
```

> Use `id` (UUID) in `selectedMediaIds` when creating/updating automations. `instagramMediaId` is the raw Instagram ID — not needed by the frontend.

> Show `mediaUrl` as the post thumbnail when `thumbnailUrl` is empty.

---

## 9. Webhooks (read-only reference)

The backend handles webhook verification and processing automatically. The frontend does not call webhook endpoints. For reference:

```
GET  /webhooks/meta   — Instagram webhook verification (Meta calls this)
POST /webhooks/meta   — Instagram webhook event receiver (Meta calls this)
```

---

## 10. Screen-to-Endpoint Map

### Dashboard screen

```
1. GET /instagram/accounts             → populate account selector dropdown
2. GET /analytics/dashboard
     ?instagramAccountId={id}          → stats cards (active automations, executions, messages, success rate)
3. GET /automations                    → automation card list
4. GET /analytics/automations
     ?instagramAccountId={id}          → join with step 3 on automationId for per-card stats
```

### Automations screen

```
1. GET /automations                    → list all automations
2. GET /analytics/automations
     ?instagramAccountId={id}          → stats per card
3. GET /instagram/{id}/media           → media picker when creating/editing (scope=SpecificPosts)
4. POST /automations                   → create
5. PUT  /automations/{id}              → edit
6. PATCH /automations/{id}/enable      → toggle on/off
7. DELETE /automations/{id}            → delete
```

### Conversations screen

```
1. GET /instagram/accounts             → account selector
2. GET /analytics/conversations
     ?instagramAccountId={id}
     &page={n}
     &pageSize=25                      → paginated conversation list
3. GET /analytics/conversations
     ?instagramAccountId={id}
     &mediaIgId={instagramMediaId}     → filter by post (optional)
```

### Analytics screen

```
1. GET /analytics/dashboard
     ?instagramAccountId={id}
     &from={date}&to={date}            → summary stats cards
2. GET /analytics/timeseries
     ?instagramAccountId={id}
     &from={date}&to={date}            → line/bar chart data
3. GET /analytics/automations
     ?instagramAccountId={id}
     &from={date}&to={date}            → per-automation breakdown table
```

### Instagram Accounts screen

```
1. GET /instagram/accounts             → list all accounts
2. POST /instagram/{id}/sync           → manual media sync
3. DELETE /instagram/{id}              → disconnect account
```

---

## 11. Enum Reference

### AutomationScope

| Value | Int | Description |
|---|---|---|
| `SpecificPosts` | 0 | Only fires on the posts you select in `selectedMediaIds` |
| `ExistingPosts` | 1 | Fires on all posts created before this automation was created |
| `FuturePosts` | 2 | Fires on all posts created after this automation was created |
| `AllPosts` | 3 | Fires on every post |

### AutomationTriggerType

| Value | Int | Description |
|---|---|---|
| `CommentKeyword` | 0 | Only fires when a comment contains one of the `keywords` |
| `AnyComment` | 1 | Fires on every comment |

### WebhookStatus (conversations)

| Value | Description |
|---|---|
| `Completed` | Automation fired and reply/DM succeeded |
| `Failed` | Automation fired but reply/DM failed after all retries |
| `Pending` | Received, not yet processed |
| `Skipped` | Own-account comment, cooldown active, or no matching automation |

### TokenStatus (Instagram account)

| Value | Action required |
|---|---|
| `active` | All good |
| `expired` | Show "token expiring soon" warning |
| `invalid` | Show "re-authenticate" prompt — user must log in again |
| `revoked` | Same as invalid |

---

## 12. Error Handling

### Success response shape

```json
{ "succeeded": true, "value": { ... } }
```

### Failure response shape

```json
{ "succeeded": false, "error": "Plan automation limit reached." }
```

### HTTP status codes

| Code | Meaning |
|---|---|
| 200 | Success |
| 400 | Validation error — check `error` field |
| 401 | Token missing or expired — refresh and retry |
| 403 | Not authorised for this resource |
| 404 | Resource not found |
| 500 | Server error — show generic error, log for debugging |

### Recommended token refresh flow

```js
async function apiCall(url, options) {
  let res = await fetch(url, { ...options, headers: authHeaders() });

  if (res.status === 401) {
    const refreshed = await refreshToken();
    if (!refreshed) { redirectToLogin(); return; }
    res = await fetch(url, { ...options, headers: authHeaders() }); // retry once
  }

  return res;
}
```

---

## 13. Plan Information

Plans are informational only — enforcement is currently **disabled** (all users have unlimited access). When enforcement is enabled in future, the API returns `"Plan automation limit reached."` errors.

| Plan | Code | Price | Automations | Accounts | Monthly Runs |
|---|---|---|---|---|---|
| Free | `free` | ₹0 | 3 | 1 | 500 |
| Starter | `starter` | ₹99/mo | 10 | 2 | 2,000 |
| Pro | `pro` | ₹299/mo | 50 | 5 | 10,000 |

---

*Generated: 2026-07-20 · KromicFlow API v1*
